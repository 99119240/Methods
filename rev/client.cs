using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static void Main()
    {
        // Replace with your ngrok-generated URL
        string ngrokUrl = "https://your-ngrok-subdomain.ngrok.io";
        int ngrokPort = 443;

        TcpClient client = new TcpClient();
        client.Connect(ngrokUrl, ngrokPort);

        Console.WriteLine("Connected to the listener.");

        while (true)
        {
            Console.Write("Enter command: ");
            string command = Console.ReadLine();

            byte[] commandBytes = Encoding.UTF8.GetBytes(command);
            NetworkStream clientStream = client.GetStream();
            clientStream.Write(commandBytes, 0, commandBytes.Length);

            // Wait for the response from the listener
            byte[] responseBytes = new byte[4096];
            int bytesRead = clientStream.Read(responseBytes, 0, 4096);
            string response = Encoding.UTF8.GetString(responseBytes, 0, bytesRead);
            Console.WriteLine($"Response: {response}");
        }
    }
}
