namespace MQTTBroker
{
    using System;
    using MQTTnet;
    using MQTTnet.Server;

    class CreateBroker
    {
        [Obsolete]
        static async System.Threading.Tasks.Task Main()
        {
            /*************************************************************
             *                    User Configuration                     *
             *************************************************************/
            var user = "IoTteam";
            var password = "daihocbachkhoa";


            //  Setup client validator                       !!! chua ket hop option builder nay vo dc 
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(99)
                .WithDefaultEndpointPort(1884);

            //  Setup client validator                 
            var options = new MqttServerOptions();
            options.ConnectionValidator = new MqttServerConnectionValidatorDelegate(c =>
            {
                if (c.Username != user)
                {
                    c.ReturnCode = MQTTnet.Protocol.MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                    return;
                }
                if (c.Password != password)
                {
                    c.ReturnCode = MQTTnet.Protocol.MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                    return;
                }
                c.ReturnCode = MQTTnet.Protocol.MqttConnectReturnCode.ConnectionAccepted;
            });

            //  Start Server                   
            var mqttServer = new MqttFactory().CreateMqttServer();
            await mqttServer.StartAsync(options);
            Console.WriteLine("Server is running... \r\n\nPress Enter to exit.");
            Console.ReadLine();
            await mqttServer.StopAsync();
        }
    }
}