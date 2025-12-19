package relay

import (
	"encoding/binary"
	"io"
	"log"
	"net"
)

// Client 구조체는 연결된 TCP 클라이언트의 정보를 담습니다.
type Client struct {
	Conn   net.Conn      // 실제 TCP 연결 객체 (소켓)
	Send   chan []byte   // 보낼 메시지를 쌓아두는 채널 (버퍼링됨)
	Server *Server       // 중앙 서버 객체에 대한 포인터 (통신용)
	RoomID string        // 클라이언트가 속한 방의 ID ("Room1", "Room2" 등)
	Stop   chan struct{} // WritePump를 강제 종료하기 위한 신호 채널
}

// ReadPump는 클라이언트로부터 들어오는 데이터를 계속 읽어서 서버로 전달(Broadcast)합니다.
// (Go 루틴으로 실행되므로 메인 로직과 별개로 동시에 돕니다.)
func (c *Client) ReadPump() {
	// defer: 이 함수(ReadPump)가 끝나면 무조건 실행되는 뒷정리 구문입니다.
	defer func() {
		close(c.Stop)                                                      // 1. WritePump에게 "일 그만하고 퇴근해"라고 신호를 보냅니다.
		c.Server.Unregister <- Unregistration{Client: c, RoomID: c.RoomID} // 2. 서버 명부에서 이름을 뺍니다.
		c.Conn.Close()                                                     // 3. 전화(TCP 연결)를 끊습니다.
	}()

	// 1. 핸드셰이크 단계 (Handshake Phase)
	// 클라이언트가 처음 접속하면 "나 어느 방 들어갈래"라고 말하는 단계입니다.
	// 패킷 형식: [길이(4바이트)][타입(1바이트)][방이름(N바이트)]

	header := make([]byte, 4) // 4바이트짜리 그릇을 만듭니다.
	if _, err := io.ReadFull(c.Conn, header); err != nil {
		log.Println("핸드셰이크 읽기 실패:", err)
		return
	}

	// 리틀 엔디안 방식으로 바이트를 정수로 변환합니다. (C# BitConverter와 호환)
	length := int32(binary.LittleEndian.Uint32(header))

	// 비정상적인 길이 체크 (해킹이나 버그 방지)
	if length <= 0 || length > 1024*1024 { // 1MB 이상의 핸드셰이크는 거절
		log.Printf("핸드셰이크 길이 오류: %d (From: %s)", length, c.Conn.RemoteAddr())
		return
	}

	body := make([]byte, length)
	if _, err := io.ReadFull(c.Conn, body); err != nil {
		log.Printf("핸드셰이크 바디 읽기 실패 (%s). 길이: %d. 에러: %v", c.Conn.RemoteAddr(), length, err)
		return
	}

	msgType := body[0]   // 첫 번째 바이트는 메시지 타입
	if msgType != 0x00 { // 0x00이 핸드셰이크 타입이라고 약속함
		log.Println("잘못된 핸드셰이크 타입:", msgType)
		return
	}

	// 나머지 바이트는 방 이름(RoomID) 문자열입니다.
	c.RoomID = string(body[1:])

	// 서버의 등록 채널(Register)에 "나 왔어요"라고 쪽지를 넣습니다.
	c.Server.Register <- Registration{Client: c, RoomID: c.RoomID}

	// 2. 릴레이 루프 (Relay Loop)
	// 이제부터 들어오는 모든 데이터는 묻지도 따지지도 않고 같은 방 사람들에게 전달합니다.
	for {
		// 헤더(길이) 읽기
		hBuf := make([]byte, 4)
		if _, err := io.ReadFull(c.Conn, hBuf); err != nil {
			break // 읽다가 에러나면 연결 끊긴 것으로 간주하고 루프 종료
		}

		dataLen := int32(binary.LittleEndian.Uint32(hBuf))

		// 바디(내용) 읽기
		bBuf := make([]byte, dataLen)
		if _, err := io.ReadFull(c.Conn, bBuf); err != nil {
			break
		}

		// 받은 그대로 다시 포장해서 (헤더 + 바디)
		fullPacket := append(hBuf, bBuf...)

		// 서버의 방송(Broadcast) 채널로 보냅니다. "이거 우리 방 애들한테 뿌려줘"
		c.Server.Broadcast(fullPacket, c)
	}
}

// WritePump는 서버가 보내준 데이터를 받아서 실제 클라이언트(소켓)에게 쏩니다.
func (c *Client) WritePump() {
	defer func() {
		c.Conn.Close() // 함수 끝나면 연결 닫기
	}()

	for {
		// select는 여러 채널을 동시에 감시하는 문법입니다. (switch case와 비슷)
		select {
		case <-c.Stop:
			// ReadPump가 "연결 끊겼어"라고 Stop 채널을 닫으면 여기로 들어옵니다.
			return // 함수 종료 (고루틴 소멸)

		case message, ok := <-c.Send: // Send 채널에 편지가 오면?
			if !ok {
				// 채널이 닫혔다는 뜻. (서버 강제 종료 등)
				c.Conn.Write([]byte{})
				return
			}

			// 실제 네트워크로 데이터 전송
			w, err := c.Conn.Write(message)
			if err != nil {
				return // 전송 실패하면 종료
			}

			// Go에서는 변수를 선언하고 안 쓰면 에러가 납니다. w를 안 쓴다고 컴파일러가 뭐라 할까봐 넣어둔 코드입니다.
			_ = w
		}
	}
}
