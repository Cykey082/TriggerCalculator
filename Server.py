#!/usr/bin/env python3
import socket
import threading


clients = []
clients_lock = threading.Lock()
# map from player name -> Client
name_map = {}

class Client:
    def __init__(self, socket: socket.socket):
        self.socket = socket
        self.name = None
        self.buffer = None
        self.opponent:Client = None  # type: Optional[Client]
        # append under lock
        with clients_lock:
            clients.append(self)

def handle_client(client: Client):
    try:
        while True:
            data = client.socket.recv(4096)
            if not data:
                break
            try:
                text = data.decode('utf-8')
            except Exception:
                text = ''
            print(f"Received raw from socket: {text}")

            # NAME registration
            if text.startswith('NAME:'):
                name = text[len('NAME:'):].strip()
                with clients_lock:
                    if name in name_map:
                        # name already taken
                        try:
                            client.socket.sendall(f"ERROR:NAME_TAKEN".encode('utf-8'))
                        except Exception:
                            pass
                        break
                    # register name
                    client.name = name
                    name_map[name] = client
                print(f"Client registered name: {name}")
                # if someone else requested to find this name, try to match
                with clients_lock:
                    for c in list(clients):
                        if getattr(c, 'requested_opponent', None) == name and c.opponent is None and client.opponent is None:
                            # match c with client
                            c.opponent = client
                            client.opponent = c
                            try:
                                c.socket.sendall(f"PEERNAME:{client.name}".encode('utf-8'))
                            except Exception:
                                pass
                            try:
                                client.socket.sendall(f"PEERNAME:{c.name}".encode('utf-8'))
                            except Exception:
                                pass
                continue

            # FIND request: want to play with target name
            if text.startswith('FIND:'):
                target = text[len('FIND:'):].strip()
                client.requested_opponent = target
                with clients_lock:
                    target_client = name_map.get(target)
                    if target_client and target_client.opponent is None and client.opponent is None:
                        # pair them
                        client.opponent = target_client
                        target_client.opponent = client
                        try:
                            client.socket.sendall(f"PEERNAME:{target_client.name}".encode('utf-8'))
                        except Exception:
                            pass
                        try:
                            target_client.socket.sendall(f"PEERNAME:{client.name}".encode('utf-8'))
                        except Exception:
                            pass
                continue

            # OPERATION: forward only to opponent if set
            if text.startswith('OPERATION:'):
                payload = text[len('OPERATION:'):]
                if client.opponent:
                    try:
                        client.opponent.socket.sendall(f"OPERATION:{payload}".encode('utf-8'))
                    except Exception:
                        try:
                            client.opponent.socket.close()
                        except Exception:
                            pass
                    continue
                else:
                    try:
                        client.socket.sendall(f"ERROR:NO_OPPONENT".encode('utf-8'))
                    except Exception:
                        pass
                    continue

            # HEARTBEAT handling or other messages
            if text.startswith('HEARTBEAT'):
                try:
                    client.socket.sendall(b'HEARTBEAT.')
                except Exception:
                    pass
                continue

    except ConnectionResetError:
        pass
    except OSError:
        pass
    finally:
        # cleanup: remove from clients and name_map, notify opponent
        with clients_lock:
            try:
                if client.name and client.name in name_map:
                    del name_map[client.name]
            except Exception:
                pass
            if client in clients:
                clients.remove(client)
            if getattr(client, 'opponent', None):
                opp = client.opponent
                client.opponent = None
                try:
                    if opp and opp.socket:
                        opp.opponent = None
                        opp.socket.sendall(b'OPPONENT_LEFT')
                except Exception:
                    pass
        try:
            client.socket.close()
        except Exception:
            pass

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('0.0.0.0', 12345))
server_socket.listen(128)
print("Server is listening on port 12345")
while True:
    try:
        client_socket, addr = server_socket.accept()
        print(f"Accepted connection from {addr}")
        client = Client(client_socket)
        threading.Thread(target=handle_client, args=(client,)).start()
    except KeyboardInterrupt:
        server_socket.close()
        break