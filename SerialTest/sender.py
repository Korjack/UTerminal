import serial
import time
import random

STX = 0x02
ETX = 0x03

def make_packet(data):
    if isinstance(data, str):
        data = data.encode('utf-8')
    elif isinstance(data, int):
        data = data.to_bytes(1, 'big')

    packet = bytes([STX]) + data + bytes([ETX])
    return packet

def sender():
    # socat으로 생성한 포트 또는 pty로 생성한 포트 사용
    port = serial.Serial('/tmp/vserial2', 9600, timeout=1)
    print("포트 연결")
    print("입력하려면 데이터를 입력하세요 (Ctrl+C로 종료):")

    try:
        while True:
            user_input = input("> ")

            # 숫자로 변환 시도, 실패하면 문자열로 처리
            try:
                data = int(user_input)
            except ValueError:
                data = user_input

            packet = make_packet(data)
            port.write(packet)
            print(f"전송: {packet}")
            time.sleep(0.1)
    except KeyboardInterrupt:
        print("Serial Port Closing...")
        port.close()
    finally:
        port.close()

if __name__ == "__main__":
    sender()