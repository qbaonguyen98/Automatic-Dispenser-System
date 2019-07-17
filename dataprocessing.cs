/**************************************************************************************
 *                 THIS CODE IS MOSTLY BASED ON MR. THONG 'S ONE                      *
 *                       A LOT OF THANKS TO MR. THONG PHAN                            *
 **************************************************************************************/      

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace MessageHandler
{
    public class MySQL
    {
        // 2 arrays below is used to anti request duplication - process a request more than once
        // Assume we have 1000 difference dispensers
        public static string[] ProcessedRequest = new string[1000];   // store Processed Request
        public static string[] ProcessedResponse = new string[1000];  // store ResponsePayload of Processed Request

        /******************************************************************************
         *                          SETUP DATABASE CONNECTION                         *
         ******************************************************************************/
        public static MySqlConnection GetDBConnection()
        {
            // CONFIGURE DATABASE USER AND PASSWORD HERE
            string connStr =    "server = localhost; " +
                                "user = root; " +
                                "database = IoT; " +
                                "port = 3306; " +
                                "password =";     
                                      
            MySqlConnection conn = new MySqlConnection(connStr);
            return conn;
        }
        /******************************************************************************
         *              DATA PROCESSING BASED ON EVERY SINGLE TOPIC                 *
         ******************************************************************************/
        /**************************
         *          ERROR         *
         **************************/
        static public string ErrorHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string error;
            string ID = PayloadAttributes["ID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];
            string ErrorCode = PayloadAttributes["ErrorCode"];

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string> 
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };

            // Error classification
            if (ErrorCode == "0")
                error = "Normal";
            else if (ErrorCode == "1")
                error = "Not Sufficient Liquor";
            else if (ErrorCode == "2")
                error = "Bottle Is Removed Without RFID";
            else if (ErrorCode == "3")
                error = "Bottle Is Installed Without RFID";
            else if (ErrorCode == "4")
                error = "RFID Reader Not Respond";
            else // (ErrorCode == "5")
                error = "Barcode Reader Not Respond";

            //Insert status to Operation_Log table
            string log = @"INSERT INTO Operation_Log (DispenserID, UID, EventType, Data, Timestamp)
                           VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";
            MySqlCommand cmd_log = new MySqlCommand(log, conn);
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = PayloadAttributes["ID"];
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = "None";
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "Error";
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value = error;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();

            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *          EVENT         *
         **************************/
        //Event = Bottle Installed => Response RemainingShot
        //Update Operation_Log
        static public string EventHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string EventCode = PayloadAttributes["EventCode"];
            string UID = PayloadAttributes["UID"];
            string ID = PayloadAttributes["ID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];
            int remain = 0;
            string ev_type;

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string> 
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };

            //Read Device_Info table
            string read = @"SELECT  Device_Info.DispenserID, 
                                    Device_Info.EstimatedRemainingShot,
                                    Device_Info.UserName,
                                    User_Info.UID 
                            FROM Device_Info 
                            JOIN User_Info 
                            ON Device_Info.UserName = User_Info.UserName";

            //Insert to Operation_Log
            string log = @"INSERT INTO Operation_Log (DispenserID, UID, EventType, Data, Timestamp) 
                           VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";

            //Reduce remaining shot and update Device_Info table
            string update = @"UPDATE Device_Info 
                              SET EstimatedRemainingShot = @EstimatedRemainingShot 
                              WHERE DispenserID = @DispenserID";
            //Update dispenser status for each event
            string update_device = @"UPDATE device_info 
                                     SET DispenserStatus = @DispenserStatus 
                                     WHERE DispenserID = @DispenserID";

            MySqlCommand cmd_log = new MySqlCommand(log, conn);                 //cmd to insert operation_log
            MySqlCommand cmd_read = new MySqlCommand(read, conn);               //cmd to query
            MySqlCommand cmd_update = new MySqlCommand(update, conn);           //update remaining shot
            MySqlCommand cmd_update_device = new MySqlCommand(update_device, conn);

            if (EventCode == "2")
            {
                ev_type = "Start Refilling";
                cmd_update_device.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_update_device.Parameters.Add("@DispenserStatus", MySqlDbType.VarChar).Value = "Filling";
                cmd_update_device.ExecuteNonQuery();
            }
            else if (EventCode == "3")
            {
                ev_type = "Cup Refilled";
                cmd_update_device.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_update_device.Parameters.Add("@DispenserStatus", MySqlDbType.VarChar).Value = "Filled";
                cmd_update_device.ExecuteNonQuery();
            }
            else if (EventCode == "4")
            {
                ev_type = "Bottle Installed";
                cmd_read.ExecuteNonQuery();                                         //start to query    
                DbDataReader reader = cmd_read.ExecuteReader();                     //reader api   
                while (reader.Read())
                {
                    if ((reader.GetString(reader.GetOrdinal("UID")) == UID) && (reader.GetString(reader.GetOrdinal("DispenserID")) == ID))
                    {
                        remain = reader.GetInt16(reader.GetOrdinal("EstimatedRemainingShot"));
                        remain--;
                        //To fill cup, remain decreases
                    }
                }
                reader.Close();                                                     //stop querying
                //Update remaining shot
                cmd_update.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_update.Parameters.Add("@EstimatedRemainingShot", MySqlDbType.Int16).Value = remain;
                cmd_update.ExecuteNonQuery();
                ResponsePayload.Add("RemainingShot", remain.ToString());
            }
            else // (EventCode == "5")
            {
                ev_type = "Bottle Removed";
                ResponsePayload.Add("RemainingShot", "0");
            }
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = UID;
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = ev_type;
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value = UID;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();
            
            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *          CARD          *
         **************************/
        //Response CardHolderName and CardHolderRole
        //Update UserName in Device_Info table that correspond the DispenserID
        static public string CardHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string UID = PayloadAttributes["UID"];
            string ID = PayloadAttributes["ID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string> 
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };
            //Default
            string user_name = "None";
            string role = "NotAuthenticated";
            string auth = "NotAuthenticated";

            int session = 0;
            int old_session = -1;
            //read user_name, role and auth from user_info
            string read = @"SELECT *
                            FROM User_Info";
            //read current user's name at the dispenser
            string read_current_user = @"SELECT UserName
                                         FROM Device_Info
                                         WHERE DispenserID = @id";
            //update previous user's name, previous user = user before card detect
            string update_pre_user = @"UPDATE Device_Info 
                                       SET PreviousUserName = @pre_user 
                                       WHERE DispenserID = @id";
            //update operation_log
            string log = @"INSERT INTO Operation_Log(DispenserID, UID, EventType, Data, Timestamp)
                            VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";
            //update device_info's user_name to current user
            string update = @"UPDATE Device_Info 
                              SET UserName = @UserName
                              WHERE DispenserID = @DispenserID";
            //get old session number, current session = old session +1
            string read_session = @"SELECT  *
                                    FROM session_info";
            //insert available information to session_info
            string insert_session = @"INSERT INTO session_info (DispenserID, Session, UserName, Timestamp)
                                      VALUES(@DispenserID, @Session, @UserName, @Timestamp)
                                      ";

            MySqlCommand cmd_read = new MySqlCommand(read, conn);
            MySqlCommand cmd_read_current_user = new MySqlCommand(read_current_user, conn);
            MySqlCommand cmd_update_pre_user = new MySqlCommand(update_pre_user, conn);
            MySqlCommand cmd_log = new MySqlCommand(log, conn);
            MySqlCommand cmd_update = new MySqlCommand(update, conn);
            MySqlCommand cmd_read_session = new MySqlCommand(read_session, conn);
            MySqlCommand cmd_insert_session = new MySqlCommand(insert_session, conn);

            cmd_read.ExecuteNonQuery();
            DbDataReader reader = cmd_read.ExecuteReader();
            while (reader.Read())
            {

                if (Convert.ToInt64(reader.GetString(reader.GetOrdinal("UID")), 16) == Convert.ToInt64(UID, 16))
                {
                    user_name = reader.GetString(reader.GetOrdinal("UserName"));
                    role = reader.GetString(reader.GetOrdinal("Role"));
                    auth = "Authenticated";
                }
            }
            reader.Close();

            // Update Previous UserName (get username and insert it to previous user name)
            cmd_read_current_user.Parameters.Add("@id", MySqlDbType.VarChar).Value = ID;
            object current_name = cmd_read_current_user.ExecuteScalar();
            if (current_name != null)
            {
                string current_user = current_name.ToString();
                cmd_update_pre_user.Parameters.Add("@id", MySqlDbType.VarChar).Value = ID;
                cmd_update_pre_user.Parameters.Add("@pre_user", MySqlDbType.VarChar).Value = current_user;
                cmd_update_pre_user.ExecuteNonQuery();
            }

            ResponsePayload.Add("AuthenticationStatus", auth);
            ResponsePayload.Add("CardHolderRole", role);
            ResponsePayload.Add("CardHolderName", user_name);

            cmd_read_session.ExecuteNonQuery();
            DbDataReader reader_session = cmd_read_session.ExecuteReader();
            if (reader_session.HasRows)
            {
                while (reader_session.Read())
                {
                    if (reader_session.GetString(reader_session.GetOrdinal("DispenserID")) == ID)
                    {
                        old_session = reader_session.GetInt16(reader_session.GetOrdinal("Session"));
                    }
                }
            }

            reader_session.Close();
            session = old_session + 1;

            //Update Device_Info table if username authenticated
            if (auth == "Authenticated")
            {
                // Compute total shot count, MUST BEFORE UPDATE USERNAME
                ShotCountHandler(conn, old_session, ID, auth);                                      
                cmd_update.Parameters.Add("@UserName", MySqlDbType.VarChar).Value = user_name;
                cmd_update.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_update.ExecuteNonQuery();
                //Insert username, session no., timestamp of current session to session_info
                cmd_insert_session.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_insert_session.Parameters.Add("@Session", MySqlDbType.Int16).Value = session;
                cmd_insert_session.Parameters.Add("@Username", MySqlDbType.VarChar).Value = user_name;
                cmd_insert_session.Parameters.Add("@Timestamp", MySqlDbType.DateTime).Value = DateTime.Now;
                cmd_insert_session.ExecuteNonQuery();
            }

            //update Operation_Log
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = UID;
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "RFID Scanned";
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value = user_name;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.DateTime).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();


            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *        BARCODE         *
         **************************/
        //Response SpiritName corresponding the Barcode received
        //Update BarcodeID in Device_Info table that correspond the DispenserID
        static public string BarcodeHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string Barcode = PayloadAttributes["Barcode"];
            string ID = PayloadAttributes["ID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string> 
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };
            //Default
            string spirit_name = "Unrecognized";
            string old_spirit_name = "None";
            int remain;
            int spirit_vol = 0;
            int dispenser_vol = 1;
            string auth = "NotAuthenticated";

            string read_spirit = "SELECT" +
                            " Spirit_Info.SpiritName," +
                            " Spirit_Info.Volume," +
                            " Spirit_Info.BarcodeID"
                        + " FROM Spirit_Info";

            string read_dispenser_vol = "SELECT Device_Info.DispenserVolume, " +
                                               "Device_Info.SpiritName " +
                                               "FROM Device_Info WHERE DispenserID = @id";

            //Insert data to Operation_Log
            string log = "INSERT INTO Operation_Log (DispenserID, UID, EventType, Data, Timestamp)"
                + " VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";

            //Update BarcodeID in Device_Info table
            string update = "UPDATE Device_Info SET BarcodeID = @BarcodeID, SpiritName = @SpiritName, EstimatedRemainingShot = @remain" +
                            " WHERE DispenserID = @DispenserID";

            MySqlCommand cmd_read_spirit = new MySqlCommand(read_spirit, conn);               //cmd to query
            MySqlCommand cmd_read_dispenser_vol = new MySqlCommand(read_dispenser_vol, conn);
       
            MySqlCommand cmd_log = new MySqlCommand(log, conn);                 //cmd to update
            MySqlCommand cmd_update = new MySqlCommand(update, conn);           //cmd to update

            // Get spirit info
            DbDataReader reader = cmd_read_spirit.ExecuteReader();                     //reader api 

            while (reader.Read())
            {
                if (reader.GetString(reader.GetOrdinal("BarcodeID")) == Barcode)
                {
                    //query informations from database
                    spirit_name = reader.GetString(reader.GetOrdinal("SpiritName"));
                    spirit_vol = Convert.ToInt16(reader.GetString(reader.GetOrdinal("Volume")));
                    auth = "Authenticated";
                }
            }
            reader.Close();                                                     //stop querying

            // Get Dispenser volume and check if bartender installed the same bottle as previous session one
            cmd_read_dispenser_vol.Parameters.Add("@id", MySqlDbType.VarChar).Value = ID;
            DbDataReader reader1 = cmd_read_dispenser_vol.ExecuteReader();
            while (reader1.Read())
            {
                dispenser_vol = Convert.ToInt16(reader1.GetString(reader1.GetOrdinal("DispenserVolume")));
                old_spirit_name = reader1.GetString(reader1.GetOrdinal("SpiritName"));
            }
            reader1.Close();

            if (old_spirit_name == spirit_name)
                ResponsePayload.Add("Compare", "0");
            else
                ResponsePayload.Add("Compare", "1");

            // Compute Estimated Remaining Shot
            remain = spirit_vol / dispenser_vol;

            ResponsePayload.Add("AuthenticationStatus", auth);
            ResponsePayload.Add("RemainingShot", remain.ToString());
            ResponsePayload.Add("SpiritName", spirit_name);

            //Update BarcodeID if it is authenticated
            if (auth == "Authenticated")
            {
                cmd_update.Parameters.Add("@BarcodeID", MySqlDbType.VarChar).Value = Barcode;
                cmd_update.Parameters.Add("@SpiritName", MySqlDbType.VarChar).Value = spirit_name;
                cmd_update.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_update.Parameters.Add("@remain", MySqlDbType.VarChar).Value = remain;
                cmd_update.ExecuteNonQuery();
            }
            //Insert status to Operation_Log
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = "None";
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "Barcode Scanned";
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value = spirit_name;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();

            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *        DISPENSING      *
         **************************/
        //Increase DispenserShot and decrease RemainingShot 
        //Update Device_Info
        static public string DispensingHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string ID = PayloadAttributes["ID"];
            string UID = PayloadAttributes["UID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string> 
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };

            string read = @"SELECT  User_Info.UID, 
                                    User_Info.UserName, 
                                    Device_Info.EstimatedRemainingShot,
                                    Device_Info.DispenserShot,
                                    Device_Info.DispenserID 
                            FROM User_Info 
                            JOIN Device_Info 
                            ON User_Info.UserName = Device_Info.UserName";

            string update = @"UPDATE Device_Info 
                              SET   DispenserShot = @DispenserShot, 
                                    EstimatedRemainingShot = @remain 
                              WHERE DispenserID = @DispenserID";

            string log = @"INSERT INTO Operation_Log(DispenserID, UID, EventType, Data, Timestamp)
                           VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";

            string update_device = @"UPDATE device_info 
                                     SET DispenserStatus = @DispenserStatus 
                                     WHERE DispenserID = @DispenserID";

            MySqlCommand cmd_read = new MySqlCommand(read, conn);
            MySqlCommand cmd_log = new MySqlCommand(log, conn);
            MySqlCommand cmd_update = new MySqlCommand(update, conn);
            MySqlCommand cmd_update_device = new MySqlCommand(update_device, conn);

            cmd_read.ExecuteNonQuery();
            DbDataReader reader = cmd_read.ExecuteReader();
            int total_shot = 0;
            int remain = 0;

            while (reader.Read())
            {
                if ((Convert.ToInt32(reader.GetString(reader.GetOrdinal("UID")), 16) == Convert.ToInt32(UID, 16)) && (reader.GetString(reader.GetOrdinal("DispenserID")) == ID))
                {
                    total_shot = reader.GetInt16(reader.GetOrdinal("DispenserShot"));
                    remain = reader.GetInt16(reader.GetOrdinal("EstimatedRemainingShot"));
                }
            }
            reader.Close();

            if (remain > 0)
            {
                total_shot++;
                remain--;
            }
            //update remain and dispensershot in device_info
            cmd_update.Parameters.Add("@DispenserShot", MySqlDbType.Int16).Value = total_shot;
            cmd_update.Parameters.Add("@remain", MySqlDbType.Int16).Value = remain;
            cmd_update.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_update.ExecuteNonQuery();
            //add requested information to response payload
            ResponsePayload.Add("SessionTotalShot", total_shot.ToString());
            ResponsePayload.Add("RemainingShot", remain.ToString());
            //update operation_log
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = UID;
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "Dispensed";
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value = "SessionTotalShot: " + total_shot;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();
            //update dispenser status to device_info
            cmd_update_device.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_update_device.Parameters.Add("@DispenserStatus", MySqlDbType.VarChar).Value = "Dispensing";
            cmd_update_device.ExecuteNonQuery();

            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *        SYNCHRONIZE     *
         **************************/
         //Response RemainingShot and SpiritName on Dispenser
        static public string SynchronizeHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string ID = PayloadAttributes["ID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string> 
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };
            //Default values
            string remain = "0";
            string spirit_name = "None";
            //query spirit name and remain form device_info and spirit_info
            string read = @"SELECT  Device_Info.DispenserID, 
                                    Device_Info.EstimatedRemainingShot, 
                                    Device_Info.BarcodeID, 
                                    Spirit_Info.SpiritName 
                            FROM    Device_Info 
                            JOIN    Spirit_Info 
                            ON      Device_Info.BarcodeID = Spirit_Info.BarcodeID";
            //update operation_log
            string log = @"INSERT INTO Operation_Log(DispenserID, UID, EventType, Data, Timestamp)
                           VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";
            //update dispenser status to Online
            string update_device = @"UPDATE device_info 
                                     SET DispenserStatus = @DispenserStatus 
                                     WHERE DispenserID = @DispenserID";

            MySqlCommand cmd_read = new MySqlCommand(read, conn);
            MySqlCommand cmd_log = new MySqlCommand(log, conn);
            MySqlCommand cmd_update_device = new MySqlCommand(update_device, conn);

            cmd_read.ExecuteNonQuery();
            DbDataReader reader = cmd_read.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(reader.GetOrdinal("DispenserID")) == ID)
                {
                    remain = reader.GetString(reader.GetOrdinal("EstimatedRemainingShot"));
                    spirit_name = reader.GetString(reader.GetOrdinal("SpiritName"));                  
                }
            }
            ResponsePayload.Add("RemainingShot", remain);
            ResponsePayload.Add("SpiritName", spirit_name);
            reader.Close();

            //Update operation_log
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = "None";
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "Synchronization";
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value =
                     "SpiritName: " + spirit_name + "\r\n" +
                     "RemainingShot: " + remain;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();
            //update dispenser status
            cmd_update_device.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_update_device.Parameters.Add("@DispenserStatus", MySqlDbType.VarChar).Value = "Online";
            cmd_update_device.ExecuteNonQuery();

            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *      UPDATE STATUS     *
         **************************/
         //Update Dispenser's Modules' Statys to operation _log
        static public string UpdateStatusHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string ID = PayloadAttributes["ID"];
            string RFIDReaderStatus = PayloadAttributes["RFIDReaderStatus"];
            string BarcodeReaderStatus = PayloadAttributes["BarcodeReaderStatus"];
            string CupStatus = PayloadAttributes["CupStatus"];
            string BottlePresenceStatus = PayloadAttributes["BottlePresenceStatus"];
            string CmdNumber = PayloadAttributes["CmdNumber"];

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string>
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };

            string read = "SELECT DispenserID FROM Device_Info";

            string update = "UPDATE Device_Info SET" +
                            " RFIDReaderStatus = @rfid_stt," +
                            " BarcodeReaderStatus = @barcode_stt," +
                            " CupStatus = @cup_stt," +
                            " BottlePresenceStatus = @bottle_stt" +
                            " WHERE DispenserID = @id";

            string log = "INSERT INTO Operation_Log(DispenserID, UID, EventType, Data, Timestamp)"
                + " VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";
            bool flag = false;
            MySqlCommand cmd_read = new MySqlCommand(read, conn);
            MySqlCommand cmd_update = new MySqlCommand(update, conn);
            MySqlCommand cmd_log = new MySqlCommand(log, conn);
            //cmd_read.ExecuteNonQuery();
            DbDataReader reader = cmd_read.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(reader.GetOrdinal("DispenserID")) == ID)
                {
                    flag = true;
                }
            }
            reader.Close();
            if (flag == true)
            {   //update status if device is valid in DB
                cmd_update.Parameters.Add("@id", MySqlDbType.VarChar).Value = ID;
                cmd_update.Parameters.Add("@rfid_stt", MySqlDbType.VarChar).Value = RFIDReaderStatus;
                cmd_update.Parameters.Add("@barcode_stt", MySqlDbType.VarChar).Value = BarcodeReaderStatus;
                cmd_update.Parameters.Add("@cup_stt", MySqlDbType.VarChar).Value = CupStatus;
                cmd_update.Parameters.Add("@bottle_stt", MySqlDbType.VarChar).Value = BottlePresenceStatus;
                cmd_update.ExecuteNonQuery();
            }

            //update operation_log
            cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = "None";
            cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "Periodic Report";
            cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value =
                 "RFIDReaderStatus: " + RFIDReaderStatus + "\r\n" +
                 "BarcodeStatus: " + BarcodeReaderStatus + "\r\n" +
                 "CupStatus: " + CupStatus + "\r\n" +
                 "BottlePresenceStatus: " + BottlePresenceStatus;
            cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
            cmd_log.ExecuteNonQuery();

            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }
        /**************************
         *   CHECK CONNECTION     *
         **************************/
         //If ID is not set, insert ID to DB
        static public string CheckConnectionHandler(MySqlConnection conn, Dictionary<string, string> PayloadAttributes)
        {
            string ID = PayloadAttributes["ID"];
            string CmdNumber = PayloadAttributes["CmdNumber"];
            //check if ID is set
            string read = @"SELECT * 
                            FROM Device_Info";
            //insert new dispenser ID with dispenservolume and set dispensershot to 0
            string insert = @"INSERT INTO  Device_Info(DispenserID, DispenserShot, DispenserVolume)
                              VALUES(@id, @DispenserShot, 30)";
            //update operation_log
            string log = @"INSERT INTO Operation_Log(DispenserID, UID, EventType, Data, Timestamp)
                           VALUES (@DispenserID, @UID, @EventType, @Data, @Timestamp)";

            bool flag = false;       // ID khac trong database
            MySqlCommand cmd_read = new MySqlCommand(read, conn);
            //MySqlCommand cmd_insert = new MySqlCommand(insert, conn);
            //MySqlCommand cmd_log = new MySqlCommand(log, conn);
            cmd_read.ExecuteNonQuery();
            DbDataReader reader = cmd_read.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (reader.GetString(reader.GetOrdinal("DispenserID")) == ID)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            else
            {
                flag = false;
            }
            reader.Close();
            if (flag == false)
            {
                MySqlCommand cmd_insert = new MySqlCommand(insert, conn);
                MySqlCommand cmd_log = new MySqlCommand(log, conn);
                cmd_insert.Parameters.Add("@id", MySqlDbType.VarChar).Value = ID;
                cmd_insert.Parameters.Add("@DispenserShot", MySqlDbType.VarChar).Value = 0;
                cmd_insert.ExecuteNonQuery();

                cmd_log.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_log.Parameters.Add("@UID", MySqlDbType.VarChar).Value = "None";
                cmd_log.Parameters.Add("@EventType", MySqlDbType.VarChar).Value = "Initialize";
                cmd_log.Parameters.Add("@Data", MySqlDbType.VarChar).Value = "None";
                cmd_log.Parameters.Add("@Timestamp", MySqlDbType.Timestamp).Value = DateTime.Now;
                cmd_log.ExecuteNonQuery();
            }

            Dictionary<string, string> ResponsePayload = new Dictionary<string, string>
            {
                { "ID", ID },
                { "CmdNumber", CmdNumber }
            };

            conn.Close();
            return JsonConvert.SerializeObject(ResponsePayload, Formatting.Indented);
        }

        /**************************
         *       SHOT COUNT       *     >>>> Called in Card Handler
         **************************/
        static public void ShotCountHandler(MySqlConnection conn, int old_session, string ID, string auth)
        {   
            //query user name, shot count and spirit name frome device info
            string read_device = @"SELECT   device_info.DispenserID,
                                            device_info.UserName, 
                                            device_info.SpiritName,         
                                            device_info.DispenserShot
                                   FROM device_info";
            //check if there was shot count value corresponding username, and spirit name
            //if there is, query old shot count
            string read_shot = @"SELECT * 
                                FROM shot_info";
            //insert new shotcount value if no value is set yet
            string insert_shot_info = @"INSERT INTO shot_info (SpiritName,UserName, ShotCount) 
                                        VALUES (@SpiritName, @UserName, @ShotCount)";
            //add old shot count to current shot count and update shot_info
            string update_shot_info = @"UPDATE shot_info 
                                        SET ShotCount = @ShotCount 
                                        WHERE SpiritName = @SpiritName 
                                        AND UserName = @UserName";
            //reset dispenser shot to 0
            string reset_dispenser_shot = @"UPDATE device_info 
                                            SET DispenserShot = 0 
                                            WHERE DispenserID = @DispenserID";
            //update spirit name and session shot to previous session
            string update_session = @"UPDATE session_info
                                      SET SpiritName = @SpiritName,
                                          SessionShot = @SessionShot
                                      WHERE DispenserID = @DispenserID
                                      AND   Session = @Session";

            MySqlCommand cmd_read_device = new MySqlCommand(read_device, conn);
            MySqlCommand cmd_read_shot = new MySqlCommand(read_shot, conn);
            MySqlCommand cmd_insert_shot_info = new MySqlCommand(insert_shot_info, conn);
            MySqlCommand cmd_update_shot_info = new MySqlCommand(update_shot_info, conn);
            MySqlCommand cmd_reset_dispenser_shot = new MySqlCommand(reset_dispenser_shot, conn);
            MySqlCommand cmd_update_session = new MySqlCommand(update_session, conn);

            string user_name = "None";
            string spirit_name = "None";

            int session_shot = 0;
            if(old_session<0)
            {
                old_session = 0;
            }

            int shot_count = 0;
            int shot_count_old = 0;
            bool shot_flag = true;

            cmd_read_device.ExecuteNonQuery();
            DbDataReader reader_device = cmd_read_device.ExecuteReader();
            while (reader_device.Read())
            {
                if (reader_device.GetString(reader_device.GetOrdinal("DispenserID")) == ID)
                {
                    user_name = reader_device.GetString(reader_device.GetOrdinal("UserName"));
                    shot_count = reader_device.GetInt16(reader_device.GetOrdinal("DispenserShot"));
                    spirit_name = reader_device.GetString(reader_device.GetOrdinal("SpiritName"));
                }
            }
            reader_device.Close();

            cmd_read_shot.ExecuteNonQuery();
            DbDataReader reader_shot = cmd_read_shot.ExecuteReader();
            if (reader_shot.HasRows)
            {
                while (reader_shot.Read())
                {
                    if ((reader_shot.GetString(reader_shot.GetOrdinal("SpiritName")) == spirit_name) && (reader_shot.GetString(reader_shot.GetOrdinal("UserName")) == user_name))
                    {
                        shot_count_old = reader_shot.GetInt16(reader_shot.GetOrdinal("ShotCount"));
                        shot_flag = false;
                    }
                }
            }
            else shot_flag = true;
            reader_shot.Close();

            session_shot = shot_count;

            shot_count = shot_count + shot_count_old;
            Console.WriteLine("User Name: {0}", user_name);
            Console.WriteLine("Shot Count: {0}", shot_count);
            Console.WriteLine("SpiritName: {0}", spirit_name);

            cmd_reset_dispenser_shot.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
            cmd_reset_dispenser_shot.ExecuteNonQuery();

            if(user_name != "None")
            {
                if (shot_flag == true)
                {
                    cmd_insert_shot_info.Parameters.Add("@SpiritName", MySqlDbType.VarChar).Value = spirit_name;
                    cmd_insert_shot_info.Parameters.Add("@UserName", MySqlDbType.VarChar).Value = user_name;
                    cmd_insert_shot_info.Parameters.Add("@ShotCount", MySqlDbType.Int16).Value = shot_count;
                    cmd_insert_shot_info.ExecuteNonQuery();
                }
                else
                {
                    cmd_update_shot_info.Parameters.Add("@ShotCount", MySqlDbType.Int16).Value = shot_count;
                    cmd_update_shot_info.Parameters.Add("@UserName", MySqlDbType.VarChar).Value = user_name;
                    cmd_update_shot_info.Parameters.Add("@SpiritName", MySqlDbType.VarChar).Value = spirit_name;
                    cmd_update_shot_info.ExecuteNonQuery();
                }


            }
            if(auth == "Authenticated")
            {
                cmd_update_session.Parameters.Add("@SpiritName", MySqlDbType.VarChar).Value = spirit_name;
                cmd_update_session.Parameters.Add("@SessionShot", MySqlDbType.Int16).Value = session_shot;
                cmd_update_session.Parameters.Add("@DispenserID", MySqlDbType.VarChar).Value = ID;
                cmd_update_session.Parameters.Add("@Session", MySqlDbType.VarChar).Value = old_session;
                cmd_update_session.ExecuteNonQuery();
            }

        }
    }
    public class GetResponse
    {
        /******************************************************************************
         *                          GET RESPONSE TOPIC                                *
         ******************************************************************************/
        static public string GetTopic(string RequestTopic)
        {
            if (RequestTopic == "Request/Error")
                return "Response/Error";
            else if (RequestTopic == "Request/Event")
                return "Response/Event";
            else if (RequestTopic == "Request/Card")
                return "Response/Card";
            else if (RequestTopic == "Request/Barcode")
                return "Response/Barcode";
            else if (RequestTopic == "Request/DispensingSpirit")
                return "Response/DispensingSpirit";
            else if (RequestTopic == "Request/Synchronize")
                return "Response/Synchronize";
            else if (RequestTopic == "Request/UpdateStatus")
                return "Response/UpdateStatus";
            else if (RequestTopic == "Request/CheckConnection")
                return "Response/CheckConnection";
            else
                return "Invalid Topic";
        }

        /******************************************************************************
         *                          GET RESPONSE PAYLOAD                              *
         ******************************************************************************/
        static public string GetPayload(string ResponseTopic, string RequestPayload, string[] ProcessedRequest, string[] ProcessedResponse)
        {
            Dictionary<string, string> PayloadAttributes = JsonConvert.DeserializeObject<Dictionary<string, string>>(RequestPayload);

            // Create the copy of request payload without "CmdNumber"
            Dictionary<string, string> temp_dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(RequestPayload);
            temp_dict.Remove("CmdNumber");
            string temp = JsonConvert.SerializeObject(temp_dict, Formatting.Indented);

            // Get Database connection
            MySqlConnection conn = MySQL.GetDBConnection();
            conn.Open();

            // Check if the message is sent for the first time <=> CmdNumber = 0
            if (PayloadAttributes["CmdNumber"] == "0")
            {
                Console.WriteLine("MESSAGE IS SENT FOR THE FIRST TIME");    // JUST FOR DEBUGGING

                // Add necessary data to response payload base on request topic
                if (ResponseTopic == "Response/Error")
                {
                    string ResponsePayload = MySQL.ErrorHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;     // save processed request to anti duplication
                    return ResponsePayload;
                }
                else if (ResponseTopic == "Response/Event")
                {
                    string ResponsePayload = MySQL.EventHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
                else if (ResponseTopic == "Response/Card")
                {
                    string ResponsePayload = MySQL.CardHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
                else if (ResponseTopic == "Response/Barcode")
                {
                    string ResponsePayload = MySQL.BarcodeHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
                else if (ResponseTopic == "Response/DispensingSpirit")
                {
                    string ResponsePayload = MySQL.DispensingHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
                else if (ResponseTopic == "Response/Synchronize")
                {
                    string ResponsePayload = MySQL.SynchronizeHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
                else if (ResponseTopic == "Response/UpdateStatus")
                {
                    string ResponsePayload = MySQL.UpdateStatusHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
                else // (ResponseTopic == "Response/CheckConnection")
                {
                    string ResponsePayload = MySQL.CheckConnectionHandler(conn, PayloadAttributes);
                    ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                    ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                    return ResponsePayload;
                }
            }
            else            //REQUEST SENT BEFORE: CmdNumber # 0
            {
                Console.WriteLine("THIS MESSAGE IS SENT AGAIN FROM DISPENSER");     // JUST FOR DEBUGGING
                
                // If this request is the same as Processed Request
                if (temp.Equals(ProcessedRequest[short.Parse(PayloadAttributes["ID"])]))      
                {
                    Console.WriteLine("Its former version was PROCESSED");      // JUST FOR DEBUGGING
                    string ResponsePayload = ProcessedResponse[short.Parse(PayloadAttributes["ID"])];
                    return ResponsePayload;
                }
                else          // This request was sent but not processed yet
                {
                    Console.WriteLine("Its former version was NOT PROCESSED");      // JUST FOR DEBUGGING
                    if (ResponseTopic == "Response/Error")
                    {
                        string ResponsePayload = MySQL.ErrorHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else if (ResponseTopic == "Response/Event")
                    {
                        string ResponsePayload = MySQL.EventHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else if (ResponseTopic == "Response/Card")
                    {
                        string ResponsePayload = MySQL.CardHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else if (ResponseTopic == "Response/Barcode")
                    {
                        string ResponsePayload = MySQL.BarcodeHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else if (ResponseTopic == "Response/DispensingSpirit")
                    {
                        string ResponsePayload = MySQL.DispensingHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else if (ResponseTopic == "Response/Synchronize")
                    {
                        string ResponsePayload = MySQL.SynchronizeHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else if (ResponseTopic == "Response/UpdateStatus")
                    {
                        string ResponsePayload = MySQL.UpdateStatusHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                    else // (ResponseTopic == "Response/CheckConnection")
                    {
                        string ResponsePayload = MySQL.CheckConnectionHandler(conn, PayloadAttributes);
                        ProcessedResponse[short.Parse(PayloadAttributes["ID"])] = ResponsePayload;
                        ProcessedRequest[short.Parse(PayloadAttributes["ID"])] = temp;
                        return ResponsePayload;
                    }
                }
            }
        }
    }
}