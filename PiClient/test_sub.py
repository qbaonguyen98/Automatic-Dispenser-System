import paho.mqtt.client as mqtt

host = "192.168.1.92" #"192.168.1.92"  
port = 1883
keepalive = 60
sub_topic = "Response/#"  # de nhan data tra ve tu server

client = mqtt.Client("laptop cua tao")
client.username_pw_set("IoTteam","daihocbachkhoa")

def on_connect(client, userdata, flags, rc):
    if rc==0:
        print("Connected to Broker")
    else:
        print("Bad connection Returned code=",rc)
    client.subscribe(sub_topic, qos = 2)
    
def on_disconnect(client, userdata, rc):
    print("Disconnecting reason  "  +str(rc))

def on_message(client, userdata, message):
    ResponseTopic = str(message.topic)
    ResponsePayload = str(message.payload)
    # Display Response Message
    print( "MESSAGE RECEIVED:")
    print( "Topic: " + ResponseTopic)
    print("Payload: " + ResponsePayload + "\r\n")

client.on_connect = on_connect
client.on_disconnect = on_disconnect
client.on_message = on_message


try:
    client.connect(host,port) #connect to broker
except:
    print("Connection Failed")
    exit(1) #Should quit or raise flag to quit or retry
client.loop_forever()