import paho.mqtt.client as mqtt
from dataprocessing import FrameDecode
from dataprocessing import FrameEncode

##############################################################################################################
#                                                  SETUP CLIENT                                              #
####################
# ##########################################################################################
host = "localhost" #"192.168.1.92"  
port = 1883
keepalive = 60
sub_topic = "Response/#"  # de nhan data tra ve tu server

client = mqtt.Client("Rasberry 1")
client.username_pw_set("IoTteam","daihocbachkhoa")

##############################################################################################################
#                                               REQUEST EXAMPLES                                             #
##############################################################################################################
#ERROR [L, ID, CMD, FT, CMDNUM(2), CODE]
#0x00 Normal
#0x01 Not sufficient liquor
#0x02 Bottle removed without RFID
#0x03 Bottle installed with out RFID
#0x04 RFID reader not response
#0x05 Barcode reader not response
error   =   [0x09, 0x01, 0x01, 0x01, 0x00, 0x00, 0x03] 

#THONG  [ 0x15, 0xab, 0x16, 0xed ]  bartender
#BAO    [ 0x69, 0xab, 0x69, 0xed ]  manager
#TAM    [ 0x96, 0xab, 0x96, 0xed ]  bartender

#EVENT [L, ID, CMD, FT, CMDNUM(2), CODE, UIDlen, UID(...)]
#0x02   Start refilling
#0x03   Cup refilled
#0x04   Bottle installed
#0x05   Bottle removed
event   =   [0x09, 0x99, 0x02, 0x01, 0x00, 0x01, 0x04, 0x04, 0x15, 0xAB, 0x16, 0xED]  # Bottle removed

#CARD [L, ID, CMD, FT, CMDNUM(2), UIDlen, UID(...)]
card    =   [0x13, 0x91, 0x03, 0x01, 0x00, 0x01, 0x04, 0x15, 0xab, 0x16, 0xed] 

#BARCODE [L, ID, CMD, FT, CMDNUM(2), BClen, BC(...)]
#Ballentine's   [ 0x15, 0x12, 0x65, 0xae ]
#Chivas Regal   [ 0x25, 0xab, 0xed, 0x32 ]
#Gin            [ 0xe9, 0x5f, 0xd2, 0x1c ]
#Hennesy        [ 0xab, 0x19, 0x52, 0xef ]
barcode =   [0x13, 0x91, 0x04, 0x01, 0x00, 0x00, 0x04, 0xab, 0x19, 0x52, 0xef]

#DISPENSING [L, ID, CMD, FT, DMNUM(2), UIDlen, UID(...)]
dispensing = [0x13, 0x91, 0x05, 0x01, 0x00, 0x00, 0x04, 0x15, 0xAB, 0x16, 0xED] 
#SYNCHRONIZE [L, ID, CMD, FT, CMDNUM(2)]
sync    =   [0x08, 0x00, 0x06, 0x01, 0x00, 0x00]
#UpdateStatus [L, ID, CMD, FT, CMDNUM(2) D0 D1 D2 D3]
update0  =   [0x12, 0x00, 0x07, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF]
update1  =   [0x12, 0x01, 0x07, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF]
update2  =   [0x12, 0x02, 0x07, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF]
#CheckConnection 
check   =   [0x05, 0x02, 0x0A, 0x01, 0x00, 0x00]


##############################################################################################################
#                                 GET ONE OF ABOVE EXAMPLES TO PUBLISH                                       #
##############################################################################################################
example0 = sync

RequestMessage0 = FrameDecode(example0)
RequestTopic0 = RequestMessage0["Topic"]
RequestPayload0 = RequestMessage0["Payload"] 


##############################################################################################################
#                                       SETUP CALLBACK FUNCTIONS                                            #
##############################################################################################################
def on_connect(client, userdata, flags, rc):
    if rc==0:
        client.connected_flag=True #set flag
        print("Connected to Broker")
    else:
        print("Bad connection Returned code=",rc)
        client.bad_connection_flag=True
    client.subscribe(sub_topic, qos = 2)
    
def on_disconnect(client, userdata, rc):
    print("Disconnecting reason  "  +str(rc))
    client.connected_flag=False
    client.disconnect_flag=True
    
def on_message(client, userdata, message):
    ResponseTopic = str(message.topic)
    ResponsePayload = str(message.payload)
    QoS = str(message.qos)
    # Display Response Message
    print( "MESSAGE RECEIVED:")
    print( "Topic: " + ResponseTopic)
    print("Payload: " + ResponsePayload)
    print("QoS: " + QoS + "\r\n")
    # PROCESSING RESPONSE MESSAGE: convert to dataframe
    DataFrame = FrameEncode(ResponseTopic, ResponsePayload)
    print( "DataFrame: " + str(DataFrame) + "\r\n")
    #client.disconnect()
    
def on_subscribe(client, userdata, mid, granted_qos):  
    print("Subscribed to topic: Response/# \r\n" )
    print( "SENDING MESSAGE")
    #print( RequestMessage )                   
    client.publish(RequestTopic0, RequestPayload0, qos=2, retain = False)    
    
client.on_connect = on_connect
client.on_disconnect = on_disconnect
client.on_message = on_message
client.on_subscribe = on_subscribe


try:
    client.connect(host,port) #connect to broker
except:
    print("Connection Failed")
    exit(1) #Should quit or raise flag to quit or retry
client.loop_forever()
