package relay

import (
	"log"
)

type BroadcastMessage struct {
	Data   []byte
	Sender *Client
}

// Registration info
type Registration struct {
	Client *Client
	RoomID string
}

// Unregistration info
type Unregistration struct {
	Client *Client
	RoomID string
}

// Server maintains the set of active clients and broadcasts messages
type Server struct {
	// rooms[roomID][client] = true
	rooms map[string]map[*Client]bool

	// Register requests from the clients.
	Register chan Registration

	// Unregister requests from clients.
	Unregister chan Unregistration

	// Inbound messages from the clients.
	broadcast chan BroadcastMessage
}

func NewServer() *Server {
	return &Server{
		Register:   make(chan Registration),
		Unregister: make(chan Unregistration),
		rooms:      make(map[string]map[*Client]bool),
		broadcast:  make(chan BroadcastMessage),
	}
}

func (s *Server) Run() {
	for {
		select {
		case reg := <-s.Register:
			// Ensure room exists
			if _, ok := s.rooms[reg.RoomID]; !ok {
				s.rooms[reg.RoomID] = make(map[*Client]bool)
			}
			s.rooms[reg.RoomID][reg.Client] = true
			log.Printf("Client joined Room [%s]. Total in room: %d", reg.RoomID, len(s.rooms[reg.RoomID]))

		case unreg := <-s.Unregister:
			if roomClients, ok := s.rooms[unreg.RoomID]; ok {
				if _, ok := roomClients[unreg.Client]; ok {
					delete(roomClients, unreg.Client)
					close(unreg.Client.Send)
					log.Printf("Client left Room [%s]. Total in room: %d", unreg.RoomID, len(roomClients))

					if len(roomClients) == 0 {
						delete(s.rooms, unreg.RoomID)
						log.Printf("Room [%s] is empty and deleted.", unreg.RoomID)
					}
				}
			}

		case message := <-s.broadcast:
			// Find the sender's room
			roomID := message.Sender.RoomID // We will need to store RoomID on Client
			if roomClients, ok := s.rooms[roomID]; ok {
				for client := range roomClients {
					// Don't send back to sender
					if client != message.Sender {
						select {
						case client.Send <- message.Data:
						default:
							// Drop packet
						}
					}
				}
			}
		}
	}
}

// Broadcast helper to be called from Client
func (s *Server) Broadcast(data []byte, sender *Client) {
	s.broadcast <- BroadcastMessage{Data: data, Sender: sender}
}
