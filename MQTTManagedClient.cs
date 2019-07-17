using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MessageHandler;
using MQTTnet.Extensions.ManagedClient;

namespace MQTTClient
{
    class Client
    {
        static async Task Main()
        {
            /*****************************************************************************
             *                             USER CONFIGURATION                            *           
             *****************************************************************************/
            string MQTTserver = "localhost";
            string clientID = "Managed Client";
            string user = "IoTteam";
            string password = "daihocbachkhoa";
            int port = 1883;
            string sub_topic = "Request/#";

            /*****************************************************************************
             *                            CREATE NEW CLIENT                              *           
             *****************************************************************************/
            var client = new MqttFactory().CreateManagedMqttClient();

            /*****************************************************************************
             *                           SETUP CLIENT OPTIONS                            *           
             *****************************************************************************/
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId(clientID)
                    .WithTcpServer(MQTTserver, port)
                    .WithCredentials(user, password)
                    //.WithTls()
                    .Build())
                .Build();

            /*****************************************************************************
             *                           DISCONNECTED HANDLER                            *           
             *****************************************************************************/
            
            client.UseDisconnectedHandler(e =>
            {
                Console.WriteLine("!!! Disconnected From Server !!!\n");
                Console.WriteLine("Attempt to reconnect .....\n");
            });
            
            /*****************************************************************************
             *                             User Configuration                            *           
             *****************************************************************************/
            client.UseConnectedHandler(e =>
            {
                Console.WriteLine("*** Server Connected ***\n");
            });

            /*****************************************************************************
             *                          INCOMING MESSAGE HANDLER                         *           
             *****************************************************************************/
            client.UseApplicationMessageReceivedHandler(e =>
            {
                string RequestTopic = e.ApplicationMessage.Topic;
                string RequestPayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                // Display Request Message
                Console.WriteLine("*** MESSAGE RECEIVED ***");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($"+ Payload = {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine();

                // Process and Response
                Console.WriteLine("PROCESSING .....");
                string ResponseTopic = GetResponse.GetTopic(RequestTopic);
                if (ResponseTopic != "Invalid Topic")
                {
                    string ResponsePayload = GetResponse.GetPayload(ResponseTopic, RequestPayload, MySQL.ProcessedRequest, MySQL.ProcessedResponse);
                    var ResponseMessage = new MqttApplicationMessageBuilder()
                       .WithTopic(ResponseTopic)
                       .WithPayload(ResponsePayload)
                       .WithExactlyOnceQoS()
                       //.WithRetainFlag()
                       .Build();
                    Console.WriteLine("\r\nRESPONDING WITH THIS BELOW MESSAGE:");
                    Console.WriteLine("Topic: {0}", ResponseTopic);
                    Console.WriteLine("Payload: {0}", ResponsePayload);

                    Task.Run(() => client.PublishAsync(ResponseMessage));
                    Console.WriteLine("DONE");
                }   
                else
                {
                    Console.WriteLine("Invalid Request Topic !");
                }
            });

            /*****************************************************************************
             *                             CONNECT TO BROKER                             *           
             *****************************************************************************/
            await client.SubscribeAsync(new TopicFilterBuilder().WithTopic(sub_topic).WithExactlyOnceQoS().Build());
            await client.StartAsync(options);
            Console.ReadLine();
        }
    }
}