#!/usr/bin/env python3
import socket
import threading


clients = []

class Client:
    def __init__(self,socket:socket.socket):
        self.socket = socket
        clients.append(self)
        self.buffer = None
def handle_client(client:Client):
    while True:
        try:
            if not client.buffer:
                buffer = client.socket.recv(1024)
                print(f"Raw received: {buffer}")
            if not buffer:
                break
            if not client.buffer:
                client.buffer = buffer
                print(f"Received: {client.buffer.decode('utf-8')}")
            # send to other clients
            if len(clients) < 2:
                # sleep
                threading.Event().wait(0.1)
                continue
            # Ensure only one thread processes the broadcast at a time
            broadcast_lock = getattr(handle_client, "broadcast_lock", None)
            if broadcast_lock is None:
                broadcast_lock = threading.Lock()
                handle_client.broadcast_lock = broadcast_lock

            if all(c.buffer for c in clients):
                with broadcast_lock:
                    if all(c.buffer for c in clients):  # Double-check inside lock
                        for c in clients:
                            buffer = c.buffer
                            for other in clients:
                                if other != c:
                                    other.socket.sendall(buffer)
                                    print(f"Sent to {other.socket.getpeername()}: {buffer.decode('utf-8')}")
                            c.buffer = None
                            buffer = None
        except ConnectionResetError:
            break
    client.socket.close()
    clients.remove(client)

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('0.0.0.0', 12345))
server_socket.listen(128)
print("Server is listening on port 12345")
while True:
    client_socket, addr = server_socket.accept()
    print(f"Accepted connection from {addr}")
    client = Client(client_socket)
    threading.Thread(target=handle_client, args=(client,)).start()