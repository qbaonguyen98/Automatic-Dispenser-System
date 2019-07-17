import paho.mqtt.client as mqtt
import time
import serial
import struct
from afproto import afproto_frame_data
from afproto import afproto_get_data
import threading 
from dataprocessing import FrameDecode
from dataprocessing import FrameEncode

START_BIT = '\x12'
STOP_BIT  = '\x13'

IS_PUBLISHED_FLAG = 1
##############################################################################################################
#                                                  SETUP CLIENT                                              #
##############################################################################################################
host = "192.168.1.92"  
port = 1883
keepalive = 60
sub_topic = "Response/#"  # de nhan data tra ve tu server
time_out = 0
client = mqtt.Client("Rasberry 1")
client.username_pw_set("IoTteam","daihocbachkhoa") 
#Define global frame   
RequestPayload=""
RequestTopic = ""
RequestMessage=""

##############################################################################################################
#                                       SETUP CALLBACK FUNCTIONS                                            #
##############################################################################################################
def on_connect(client, userdata, flags, rc):
    print("Connectted")
    
def on_disconnect(client, userdata, rc):
    print("Disconnecting reason  "  +str(rc))
    client.connected_flag=False
    client.disconnect_flag=True
    
def on_message(client, userdata, message):
    ResponseTopic = str(message.topic)
    ResponsePayload = str(message.payload)
    # Display Response Message
    print "MESSAGE RECEIVED:"
    print "Topic: " + ResponseTopic
    print "Payload: " + ResponsePayload + "\r\n"
    # PROCESSING RESPONSE MESSAGE: convert to dataframe
    DataFrame = FrameEncode(ResponseTopic, ResponsePayload)
    print "DataFrame: " + str(DataFrame) + "\r\n"
    serial_write_data(DataFrame)
  
def on_log(client, userdata, level, buf):
   print("log: ",buf)

def on_publish(client, userdata, mid):
   global IS_PUBLISHED_FLAG
   IS_PUBLISHED_FLAG =1
   print("messeage is published ")

def x_client_public(x_topic,x_message,x_qos):
   global IS_PUBLISHED_FLAG
   # print (IS_PUBLISHED_FLAG)
   print("publishing topic and waiting for it finish")
   print("Request Topic: " + x_topic +"\r\n")
   print("Request Payload: " + x_message +"\r\n")
   client.publish(x_topic,x_message,qos=x_qos,retain=False)
   IS_PUBLISHED_FLAG=0   
   while (True):  
      time.sleep(0.001)
      if (IS_PUBLISHED_FLAG == 1):
         break
def serial_data_handler(frame=[], *args):
    global example
    global RequestMessage
    global RequestPayload
    global RequestTopic
    example = frame
    ##############################################################################################################
    #                                 GET ONE OF ABOVE EXAMPLES TO PUBLISH                                       #
    ##############################################################################################################
    RequestMessage = FrameDecode(example)
    RequestTopic = RequestMessage["Topic"]
    RequestPayload = RequestMessage["Payload"]
    x_client_public(RequestTopic,RequestPayload,2)
   # client.publish(RequestTopic, RequestPayload, qos=2, retain = False)    
def serial_write_data(get_frame=[], *args):
    len_data = len(get_frame)
    # print(len_data)
    frame = struct.pack('B'*len_data,*get_frame)
    encoded_frame = afproto_frame_data(frame)
    ser.write(encoded_frame)
def serial_read_data():
    while(True):
        IS_Read    = 0
        IS_Started = 0 
        frame_recevied = ""
        while (True):
            raw_data = ser.read()
            if raw_data == START_BIT:
               #  print("after start bit ")
                IS_Started=1
            if IS_Started == 1 :
                frame_recevied += raw_data
               #  print(raw_data)
            if raw_data == STOP_BIT:
               #  print("stop")
                break
        data_decode,raw_frame_data = afproto_get_data(frame_recevied)
        data_convert=[]
        if (data_decode != None):
            print("crc matched and jump into handler")
            leng = len(data_decode)
            for i in range (leng) :
                data_convert.append(ord(data_decode[i]))
            print(data_convert)
            serial_data_handler(data_convert)

#serial port init #depend on serial port is usb or GPIO
# ser = serial.Serial(port='/dev/ttyS0',     # GPIO 
ser = serial.Serial(port='/dev/ttyUSB0',    #USB
               baudrate = 115200,
               parity=serial.PARITY_NONE,
               stopbits=serial.STOPBITS_ONE,
               bytesize=serial.EIGHTBITS,
               timeout=1)
print("Serial Initial Successful")



client.on_message = on_message
# client.on_log = on_log
client.on_publish = on_publish
client.on_connect = on_connect

client.connect(host,port,keepalive)
client.subscribe(sub_topic, qos = 2)
client.loop_start()

serial_data_thread = threading.Thread(target=serial_read_data)
serial_data_thread.start()

while(True):
   time.sleep(1)
      