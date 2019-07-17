import json

def GetChildTopic(DataFrame):
    if DataFrame[2] == 0x01:
        return "Error"
    elif DataFrame[2] == 0x02:
        return "Event"
    elif DataFrame[2] == 0x03:
        return "Card"
    elif DataFrame[2] == 0x04:
        return "Barcode"
    elif DataFrame[2] == 0x05:
        return "DispensingSpirit"
    elif DataFrame[2] == 0x06:
        return "Synchronize"
    elif DataFrame[2] == 0x07:
        return "UpdateStatus"
    #elif DataFrame[2] == 0x08:         # TO DO, NOT THIS VERSION
    #    return "LockBottle"
    elif DataFrame[2] == 0x08:
        return "CheckConnection"

def GetTopic(DataFrame):
    if DataFrame[3] == 0x01:
        topic = "Request/" + GetChildTopic(DataFrame)
    else:
        topic = "Ping"              # not used in this version
    return topic

def GetPayload(topic, DataFrame):
    ID = DataFrame[1]
    CmdNumber = (DataFrame[4]<<8)|(DataFrame[5])
    payload = {
        "ID": ID,
        "CmdNumber": CmdNumber
        }
    if topic == "Request/Error":
        payload["ErrorCode"] = DataFrame[6]

    elif topic == "Request/Event":       
        payload["EventCode"] = DataFrame[6]
        UIDlength = DataFrame[7]
        UID = 0
        for i in range(UIDlength):
            UID = UID | (DataFrame[8+i]<<(8*(UIDlength-i-1)))
        payload["UID"] = hex(UID).rstrip("L")

    elif topic == "Request/Card":
        UIDlength = DataFrame[6]
        UID = 0
        for i in range(UIDlength):
            UID = UID | (DataFrame[7+i]<<(8*(UIDlength-i-1)))
        payload["UID"] = hex(UID).rstrip("L")

    elif topic == "Request/Barcode":
        Barcodelength = DataFrame[6]
        Barcode = 0
        for i in range(Barcodelength):       # i = 0.....(Barcodelength - 1)
            Barcode = Barcode | (DataFrame[7+i]<<(8*(Barcodelength-i-1)))         
        payload["Barcode"] = hex(Barcode).rstrip("L")

    elif topic == "Request/DispensingSpirit":
        UIDlength = DataFrame[6]
        UID = 0
        for i in range(UIDlength):
            UID = UID | (DataFrame[7+i]<<(8*(UIDlength-i-1)))
        payload["UID"] = hex(UID).rstrip("L")

    elif topic == "Request/Synchronize":
        payload = payload

    elif topic == "Request/UpdateStatus":
        if DataFrame[6] == 0x00:
            payload["RFIDReaderStatus"] = "OFF"
        else:
            payload["RFIDReaderStatus"] = "ON"
        if DataFrame[7] == 0x00:
            payload["BarcodeReaderStatus"] = "OFF"
        else:
            payload["BarcodeReaderStatus"] = "ON"
        if DataFrame[8] == 0x00:
            payload["CupStatus"] = "NOTFILLED"
        else:
            payload["CupStatus"] = "FILLED"
        if DataFrame[9] == 0x00:
            payload["BottlePresenceStatus"] = "NOTAVAILABLE"
        else:
            payload["BottlePresenceStatus"] = "AVAILABLE"

    elif topic == "Request/CheckConnection":
        payload = payload

    return json.dumps(payload)