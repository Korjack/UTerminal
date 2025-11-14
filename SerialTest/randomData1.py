import random
import struct
import serial
import time

def generate_random_serial_data():
    STX = 0x02
    ETX = 0x03

    random_byte = random.randint(0, 255)
    data_length = random.randint(1, 50)
    a_data = bytes([random.randint(0, 255) for _ in range(data_length)])

    packet = struct.pack('B', STX)
    packet += struct.pack('B', random_byte)
    packet += struct.pack('B', data_length)
    packet += a_data
    packet += struct.pack('B', ETX)

    return packet


def main():
    port = '/tmp/vserial2'  # 포트 번호 수정
    baudrate = 9600

    try:
        with serial.Serial(port, baudrate, timeout=1) as ser:
            print(f"Serial port {port} opened")
            print("Sending data every 0.01s... Press Ctrl+C to stop")

            count = 0
            while True:
                data = generate_random_serial_data()
                ser.write(data)
                count += 1

                if count % 100 == 0:
                    print(f"Sent {count} packets - Last: {data.hex()}")

                time.sleep(0.5)

    except KeyboardInterrupt:
        print(f"\nStopped. Total sent: {count} packets")
    except serial.SerialException as e:
        print(f"Serial error: {e}")


if __name__ == "__main__":
    main()