package main

import (
	"drawing-relay/relay"
	"flag"
	"log"
	"net"
)

var addr = flag.String("addr", ":9000", "http service address")

func main() {
	flag.Parse()

	// Create the hub
	server := relay.NewServer()
	go server.Run()

	listener, err := net.Listen("tcp", *addr)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
	}
	defer listener.Close()

	log.Printf("Relay Server started on %s", *addr)

	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Println("Accept error:", err)
			continue
		}

		client := &relay.Client{
			Conn:   conn,
			Server: server,
			Send:   make(chan []byte, 256),
		}

		// server.Register <- client
		// Registration happens in ReadPump after handshake

		// Serve in new goroutines
		go client.WritePump()
		go client.ReadPump()
	}
}
