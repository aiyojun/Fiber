import socket
import threading

def receive_messages(sock):
    """在单独线程中接收服务器消息"""
    while True:
        try:
            data = sock.recv(1024).decode('utf-8')
            if not data:
                print("服务器断开连接")
                break
            print(f"服务器: {data}")
        except:
            print("与服务器的连接已断开")
            break

def tcp_client_advanced():
    server_host = '127.0.0.1'
    server_port = 9877
    
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    
    try:
        print(f"正在连接服务器 {server_host}:{server_port}...")
        client_socket.connect((server_host, server_port))
        print("连接成功！")
        print("输入消息发送给服务器，输入 'quit' 退出")
        
        # 启动接收消息的线程
        receive_thread = threading.Thread(target=receive_messages, args=(client_socket,))
        receive_thread.daemon = True
        receive_thread.start()
        
        while True:
            message = input("请输入: ")
            
            if message.lower() == 'quit':
                print("退出客户端")
                break
            print("用户输入了:", message)
            if len(message) == 0:
                message = '\n'
            client_socket.send((message).encode('utf-8'))
            
    except Exception as e:
        print(f"发生错误: {e}")
    finally:
        client_socket.close()

if __name__ == "__main__":
    tcp_client_advanced()