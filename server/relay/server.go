package relay

import (
	"log"
)

// BroadcastMessage는 방송할 데이터와 보낸 사람 정보를 담는 구조체입니다.
type BroadcastMessage struct {
	Data   []byte  // 보낼 데이터 (바이트 배열)
	Sender *Client // 누가 보냈는지 (자기가 보낸 건 자기한테 다시 안 보내려고 필요함)
}

// Registration 정보 (입장 신청서)
type Registration struct {
	Client *Client // 입장하려는 클라이언트
	RoomID string  // 들어가려는 방 이름
}

// Unregistration 정보 (퇴장 신청서)
type Unregistration struct {
	Client *Client // 나가려는 클라이언트
	RoomID string  // 어느 방에서 나가는지
}

// Server는 접속한 모든 클라이언트를 관리하고 메시지를 중계하는 중앙 허브입니다.
type Server struct {
	// rooms는 방 별로 클라이언트들이 누구누구 있는지 기록하는 출석부입니다.
	// 구조: 맵[방이름] -> (맵[클라이언트] -> true/false)
	// 예: "Room1" -> {철수: true, 영희: true}
	rooms map[string]map[*Client]bool

	// Register는 클라이언트가 "나 입장할래요"라고 소리치는 채널입니다.
	Register chan Registration

	// Unregister는 클라이언트가 "나 나갈래요"라고 소리치는 채널입니다.
	Unregister chan Unregistration

	// broadcast는 "이 메시지 모두에게 뿌려주세요"라고 요청하는 채널입니다.
	broadcast chan BroadcastMessage
}

// NewServer는 새로운 서버 객체를 만듭니다. (채널들도 이때 초기화해줍니다)
func NewServer() *Server {
	return &Server{
		Register:   make(chan Registration),
		Unregister: make(chan Unregistration),
		rooms:      make(map[string]map[*Client]bool),
		broadcast:  make(chan BroadcastMessage),
	}
}

// Run은 서버의 메인 루프로, 끊임없이 채널들을 감시하며 일을 처리합니다.
// 이 함수는 고루틴으로 실행되므로 멈추지 않고 계속 돕니다.
func (s *Server) Run() {
	for {
		// select는 여러 채널 중 하나라도 신호가 오면 즉시 처리합니다.
		select {
		case reg := <-s.Register: // 입장 요청이 오면
			// 1. 방이 없으면 새로 만듭니다.
			if _, ok := s.rooms[reg.RoomID]; !ok {
				s.rooms[reg.RoomID] = make(map[*Client]bool)
			}
			// 2. 출석부에 이름을 적습니다.
			s.rooms[reg.RoomID][reg.Client] = true
			log.Printf("클라이언트 입장 [%s]. 현재 인원: %d명", reg.RoomID, len(s.rooms[reg.RoomID]))

		case unreg := <-s.Unregister: // 퇴장 요청이 오면
			if roomClients, ok := s.rooms[unreg.RoomID]; ok { // 방이 존재하고
				if _, ok := roomClients[unreg.Client]; ok { // 명단에 이름이 있으면
					delete(roomClients, unreg.Client) // 1. 명단에서 지웁니다.
					close(unreg.Client.Send)          // 2. 클라이언트의 편지함(채널)을 닫습니다. (더 이상 메시지 못 받게)
					log.Printf("클라이언트 퇴장 [%s]. 현재 인원: %d명", unreg.RoomID, len(roomClients))

					// 3. 방에 아무도 없으면 방을 없앱니다. (메모리 절약)
					if len(roomClients) == 0 {
						delete(s.rooms, unreg.RoomID)
						log.Printf("방 [%s]이 비어서 삭제되었습니다.", unreg.RoomID)
					}
				}
			}

		case message := <-s.broadcast: // 방송 요청이 오면
			// 1. 보낸 사람이 어느 방인지 확인합니다.
			roomID := message.Sender.RoomID

			// 2. 그 방에 있는 사람 명단을 가져옵니다.
			if roomClients, ok := s.rooms[roomID]; ok {
				for client := range roomClients {
					// 3. 보낸 사람 본인이 아니면 메시지를 보냅니다.
					if client != message.Sender {
						// select default 구문은 "혹시 꽉 차서 못 보내면 그냥 버려라"는 뜻입니다.
						// (느린 클라이언트 때문에 서버 전체가 멈추는 것을 방지하는 안전장치)
						select {
						case client.Send <- message.Data:
						default:
							// 패킷 드랍 (Packet Drop)
						}
					}
				}
			}
		}
	}
}

// Broadcast는 Client가 좀 더 편하게 함수처럼 호출하라고 만든 도우미 함수입니다.
func (s *Server) Broadcast(data []byte, sender *Client) {
	s.broadcast <- BroadcastMessage{Data: data, Sender: sender}
}
