using System;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;

public class Tutorial1
{
    public static void Main()
    {
        string connStr = "server = localhost; user = root; port = 3306; password = ''";
        MySqlConnection conn = new MySqlConnection(connStr);
        try
        {
            Console.WriteLine("Connecting to MySQL and create database...");
            conn.Open();
            /**************************************************************
             *              ADD DATABASE OPERATIONS HERE                  *
             **************************************************************/

            string sql =
                @"
                    drop database if exists IoT;
                    create database IoT;
                    use IoT;
                    create table Device_Info (
                        DispenserID int,
                        BarcodeID varchar(255) DEFAULT 'None',
                        SpiritName varchar(255) DEFAULT 'None',
                        PreviousUserName varchar(255) DEFAULT 'None',
                        UserName varchar(255) DEFAULT 'None',
                        DispenserVolume int DEFAULT 30,
                        EstimatedRemainingShot int DEFAULT 0,
                        DispenserShot int DEFAULT 0,
                        DispenserStatus varchar(255) DEFAULT 'Offline',
                        RFIDReaderStatus varchar(255) DEFAULT 'None',
                        BarcodeReaderStatus varchar(255) DEFAULT 'None',
                        CupStatus varchar(255) DEFAULT 'None',
                        BottlePresenceStatus varchar(255) DEFAULT 'None'
                    );
                    CREATE TABLE Spirit_Info (
                        BarcodeID VARCHAR(255),
                        SpiritName VARCHAR(255),
                        Volume int
                    );
                    create table User_Info (
                        UID varchar(255),
                        UserName varchar(255),
                        Role varchar(255)
                    );
                    create table Operation_Log(
                        DispenserID varchar(255),
                        UID varchar(255),
                        EventType varchar(255),
                        Data varchar(255),
                        Timestamp varchar(255),
                        Result varchar(255)
                    );
                    create table Shot_info(
                        SpiritName varchar(255),
                        UserName varchar(255),
                        ShotCount int
                    );
                    create table session_info(
                        DispenserID varchar(255),
                        Session int,
                        UserName varchar(255),
                        SpiritName varchar(255),
                        SessionShot int,
                        Timestamp varchar(255)
                    );
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x30353633323434313934', 'Remy Martin', 750); 
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x36393334383937373032353931', 'Chivas Regal', 750); 
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x35303030323637303234303131', 'Black Label', 750); 

                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x31323334353637383930313238', 'Bourbon', 750); 
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x33343536373839303132333430', 'Vermouth', 750); 
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x39373830323031333739363234', 'Merlot', 750); 
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x39373831323334353637383937', 'Volka', 750); 
                    insert into Spirit_Info(BarcodeID, SpiritName, Volume) values('0x41424361626331323334', 'Latour', 750); 

                    insert into User_Info(UID, UserName, Role) values('0x97e8340c', 'Nguyen Quoc Bao', 'MANAGER');
                    insert into User_Info(UID, UserName, Role) values('0x6445c10d', 'Phan Tri Thong', 'BARTENDER');
                    insert into User_Info(UID, UserName, Role) values('0x032b3ead', 'Nguyen Thanh Tam', 'BARTENDER');

                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('0','0x30353633323434313934', 'Remy Martin', 'None', 30, 99,0);
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('1','0x36393334383937373032353931', 'Chivas Regal', 'None', 30, 99,0);
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('2','0x35303030323637303234303131', 'Black Label', 'None', 30, 99,0);
                    
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('3','0x31323334353637383930313238', 'Bourbon', 'None', 30, 99,0);
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('4','0x33343536373839303132333430', 'Vermouth', 'None', 30, 99,0);
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('5','0x39373830323031333739363234', 'Merlot', 'None', 30, 99,0);
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('6','0x39373831323334353637383937', 'Volka', 'None', 30, 99,0);
                    insert into device_info(DispenserID,BarcodeID, SpiritName, UserName, DispenserVolume, EstimatedRemainingShot, DispenserShot)
                    values ('7','0x41424361626331323334', 'Latour', 'None', 30, 99,0);
                ";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();

        }
        /**************************************************************
         *          ERROR NOTIFICATION: don't need to change          *
         **************************************************************/
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        conn.Close();
        Console.WriteLine("SUCCESSFUL");
        Console.ReadLine();
    }
}