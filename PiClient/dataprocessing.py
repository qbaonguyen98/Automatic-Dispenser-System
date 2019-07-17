from mqttdata import GetTopic
from mqttdata import GetPayload

from framedata import Error_FrameEncode
from framedata import Event_FrameEncode
from framedata import Card_FrameEncode
from framedata import Barcode_FrameEncode
from framedata import DispensingSpirit_FrameEncode
from framedata import Synchronize_FrameEncode
from framedata import UpdateStatus_FrameEncode
from framedata import CheckConnection_FrameEncode

# len   id  cmd   ft  cmd_num     data

error_example =             [0x09, 0x01, 0x01, 0x01, 0x00, 0x00, 0x02]  # Bottle is removed without RFID
event_example =             [0x09, 0x02, 0x02, 0x01, 0x00, 0x00, 0x05, 0x04, 0x69, 0xAB, 0x69, 0xED]  # Bottle removed
card_example =              [0x13, 0x93, 0x03, 0x01, 0x00, 0x00, 0x04, 0xFE, 0xA0, 0xB2, 0xC7]   # 4 bytes UID = FE A0 B2 C7
barcode_example =           [ 0x13, 0x94, 0x04, 0x01, 0x00, 0x00, 0x04, 0x15, 0x12, 0x65, 0xae]  # 4 bytes barcode = E9 5F D2 1C
dispensing_example =        [ 0x13, 0x95, 0x05, 0x01, 0x00, 0x00, 0x04, 0x50, 0x4E, 0x13, 0x7F]  # 4 bytes RFID = 50 4E 13 7F
synchronize_example =       [ 0x08, 0x96, 0x06, 0x01, 0x00, 0x00]
updatestatus_example =      [ 0x12, 0x97, 0x07, 0x01, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF]
checkconnection_example =   [ 0x08, 0x99, 0x09, 0x01, 0x00, 0x00]


def FrameDecode(DataFrame):
    RequestTopic = GetTopic(DataFrame)
    RequestPayload = GetPayload(RequestTopic, DataFrame)
    RequestMessage = dict()
    RequestMessage["Topic"] = RequestTopic
    RequestMessage["Payload"] = RequestPayload
    return RequestMessage

def FrameEncode(ResponseTopic, ResponsePayload):            # UPDATING ............
    if ResponseTopic == "Response/Error":
        DataFrame = Error_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/Event":
        DataFrame = Event_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/Card":
        DataFrame = Card_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/Barcode":
        DataFrame = Barcode_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/DispensingSpirit":
        DataFrame = DispensingSpirit_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/Synchronize":
        DataFrame = Synchronize_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/UpdateStatus":
        DataFrame = UpdateStatus_FrameEncode(ResponsePayload)
    elif ResponseTopic == "Response/CheckConnection":
        DataFrame = CheckConnection_FrameEncode(ResponsePayload)
    return DataFrame
    





# JUST FOR TESTING
#print "\r\n"
#print GetTopic(error_example)
#print GetPayload(GetTopic(error_example), error_example)
#print "\r\n"
#print GetTopic(event_example)
#print GetPayload(GetTopic(event_example), event_example)
#print "\r\n"
#print GetTopic(card_example)
#print GetPayload(GetTopic(card_example), card_example)
#print "\r\n"
#print GetTopic(barcode_example)
#print GetPayload(GetTopic(barcode_example), barcode_example)
#print "\r\n"
#print GetTopic(dispensing_example)
#print GetPayload(GetTopic(dispensing_example), dispensing_example)
#print "\r\n"
#print GetTopic(synchronize_example)
#print GetPayload(GetTopic(synchronize_example), synchronize_example)
#print "\r\n"
#print GetTopic(updatestatus_example)
#print GetPayload(GetTopic(updatestatus_example), updatestatus_example)
#print "\r\n"
#print GetTopic(checkconnection_example)
#print GetPayload(GetTopic(checkconnection_example), checkconnection_example)




