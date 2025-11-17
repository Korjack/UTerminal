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

            tokens = user_input.split()
            data_bytes = bytearray()

            for token in tokens:
                try:
                    # 0x로 시작하면 16진수로 파싱
                    if token.startswith('0x') or token.startswith('0X'):
                        value = int(token, 16)
                        # 0-255 범위 체크
                        if 0 <= value <= 255:
                            data_bytes.append(value)
                        else:
                            print(f"경고: {token}은 0-255 범위를 벗어남")
                    else:
                        # 10진수로 시도
                        value = int(token)
                        if 0 <= value <= 255:
                            data_bytes.append(value)
                        else:
                            print(f"경고: {token}은 0-255 범위를 벗어남")
                except ValueError:
                    # 숫자가 아니면 ASCII 문자열로 처리
                    data_bytes.extend(token.encode('utf-8'))

            packet = make_packet(bytes(data_bytes))
            port.write(packet)
            print(f"전송: {packet.hex(' ')}")
            time.sleep(0.1)
    except KeyboardInterrupt:
        print("Serial Port Closing...")
        port.close()
    finally:
        port.close()

if __name__ == "__main__":
    sender()