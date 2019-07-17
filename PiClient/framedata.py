import json

def AddNameBytes(DataFrame, str):
    for char in str:
        DataFrame.append(ord(char))
    return DataFrame

# ACK byte = 0x00: reserved for future use
# CMDNUMBER (2 bytes): 0x00 0xxx

def Error_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    # L     ID      CMD     FT      CMDNUMBER       ACK
    DataFrame = [0x06, ID, 0x01, 0x02, 0x00, CmdNumber, 0x00]       
    return DataFrame

def Event_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    # L     ID      CMD     FT      CMNNUMBER       REMAINING SHOT (if exist)   ACK
    if "RemainingShot" in temp:
        RemainingShot = int(temp["RemainingShot"]) 
        DataFrame = [0x07, ID, 0x02, 0x02, 0x00, CmdNumber, RemainingShot, 0x00]
    else:
        DataFrame = [0x06, ID, 0x02, 0x02, 0x00, CmdNumber, 0x00]        
    return DataFrame

def Card_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    if (temp["AuthenticationStatus"] == "Authenticated"):
        AuthenticationStatus = 0x00
    else:
        AuthenticationStatus = 0x01

    if (temp["CardHolderRole"] == "MANAGER"):
        CardHolderRole = 0x02
    elif (temp["CardHolderRole"] == "BARTENDER"):
        CardHolderRole = 0x01
    else:
        CardHolderRole = 0x00

    CardHolderName = temp["CardHolderName"]
    NameBytes = CardHolderName.encode('utf-8')
    NameLength = len(NameBytes)

    FrameLength = 8 + NameLength
    # L     ID      CMD     FT      CMDNUMBER   ACK       AuStatus        Role        Name
    DataFrame = [FrameLength, ID, 0x03, 0x02, 0x00, CmdNumber, 0x00, AuthenticationStatus, CardHolderRole]
    DataFrame = AddNameBytes(DataFrame, CardHolderName)             
    return DataFrame

def Barcode_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    if (temp["AuthenticationStatus"] == "Authenticated"):
        AuthenticationStatus = 0x00
    else:
        AuthenticationStatus = 0x01
    RemainingShot = int(temp["RemainingShot"])
    Compare = int(temp["Compare"])
    SpiritName = temp["SpiritName"]
    NameBytes = SpiritName.encode('utf-8')
    NameLength = len(NameBytes)

    FrameLength = 8 + NameLength     
    # L     ID      CMD     FT      CMDNUMBER   ACK       AuStatus        Compare        RemainingShot      (no name length)      SpiritName
    DataFrame = [FrameLength, ID, 0x04, 0x02, 0x00, CmdNumber, 0x00, AuthenticationStatus, Compare, RemainingShot]
    DataFrame = AddNameBytes(DataFrame, SpiritName)
    return DataFrame

def DispensingSpirit_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    SessionTotalShot = int(temp["SessionTotalShot"])
    RemainingShot = int(temp["RemainingShot"])
    # L     ID      CMD     FT      CMDNUMBER   ACK       SessionTotalShot        RemainingShot
    DataFrame = [0x08, ID, 0x05, 0x02, 0x00, CmdNumber, 0x00, SessionTotalShot, RemainingShot]
    return DataFrame

def Synchronize_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    #DispenserID = int(temp["DispenserID"])
    RemainingShot = int(temp["RemainingShot"])

    SpiritName = temp["SpiritName"]
    NameBytes = SpiritName.encode('utf-8')
    NameLength = len(NameBytes)
    
    FrameLength = 7 + NameLength
    # L     ID      CMD     FT      CMDNUMBER   ACK         RemainingShot       SpiritName       
    DataFrame = [FrameLength, ID, 0x06, 0x02, 0x00, CmdNumber, 0x00, RemainingShot]
    DataFrame = AddNameBytes(DataFrame, SpiritName)
    return DataFrame

def UpdateStatus_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    # L     ID      CMD     FT      CMDNUMBER   ACK       
    DataFrame = [0x06, ID, 0x07, 0x02, 0x00, CmdNumber, 0x00]
    return DataFrame

def CheckConnection_FrameEncode(Payload):
    temp = json.loads(Payload)      # parse JSON payload
    ID = int(temp["ID"])
    CmdNumber = int(temp["CmdNumber"])
    # L     ID      CMD     FT      CMDNUMBER   ACK      
    DataFrame = [0x06, ID, 0x08, 0x02, 0x00, CmdNumber, 0x00]
    return DataFrame
