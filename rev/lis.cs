using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static void Main()
    {
        // Specify ngrok settings
        int localPort = 1234;
        string ngrokAuthToken = "YourNgrokAuthToken"; // Replace with your ngrok auth token

        // Start ngrok to expose a local port
        NgrokTunnelInfo ngrokInfo = StartNgrok(localPort, ngrokAuthToken);

        // Allow time for ngrok to establish the tunnel
        Console.WriteLine("Press Enter when ngrok tunnel is ready.");
        Console.ReadLine();

        // Use the generated ngrok URL for communication
        Console.WriteLine($"Listening on {ngrokInfo.PublicUrl}");

        TcpListener listener = new TcpListener(IPAddress.Any, localPort);
        listener.Start();

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    static void HandleClient(TcpClient tcpClient)
    {
        NetworkStream clientStream = tcpClient.GetStream();

        while (true)
        {
            byte[] message = new byte[4096];
            int bytesRead;

            try
            {
                bytesRead = clientStream.Read(message, 0, 4096);
            }
            catch
            {
                break;
            }

            if (bytesRead == 0)
                break;

            string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
            Console.WriteLine($"Received: {receivedMessage}");

            // Execute the received command (replace this with your desired logic)
            string responseMessage = ExecuteCommand(receivedMessage);
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);
            clientStream.Write(responseBytes, 0, responseBytes.Length);
        }

        tcpClient.Close();
    }

    static NgrokTunnelInfo StartNgrok(int localPort, string authToken)
    {
        // Check if ngrok executable is present
        string ngrokPath = "ngrok.exe";
        if (!File.Exists(ngrokPath))
        {
            // Download ngrok executable
            Console.WriteLine("Downloading ngrok...");
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile("https://bin.equinox.io/c/4VmDzA7iaHb/ngrok-stable-windows-amd64.zip", "ngrok.zip");
            }

            // Extract ngrok executable
            Console.WriteLine("Extracting ngrok...");
            System.IO.Compression.ZipFile.ExtractToDirectory("ngrok.zip", ".");
            System.IO.File.Delete("ngrok.zip");
        }

        // Generate a random subdomain
        string randomSubdomain = Guid.NewGuid().ToString().Substring(0, 8);

        // Start ngrok process to expose localPort with the generated subdomain
        Process ngrokProcess = new Process();
        ngrokProcess.StartInfo.FileName = ngrokPath;
        ngrokProcess.StartInfo.Arguments = $"http {localPort} --authtoken {authToken} --subdomain={randomSubdomain}";
        ngrokProcess.StartInfo.UseShellExecute = false;
        ngrokProcess.StartInfo.RedirectStandardOutput = true;
        ngrokProcess.Start();

        // Wait for ngrok to fetch public URL
        Thread.Sleep(5000); // Adjust the delay based on your network speed and ngrok startup time

        // Read ngrok public URL from output
        string ngrokOutput = ngrokProcess.StandardOutput.ReadToEnd();
        string publicUrl = ngrokOutput.Split('\n')[1].Trim(); // Assuming ngrok URL is on the second line

        Console.WriteLine($"ngrok output: {ngrokOutput}");

        return new NgrokTunnelInfo(publicUrl);
    }

    static string ExecuteCommand(string command)
    {
        // Replace this with your own command execution logic
        // For demonstration purposes, let's just return a response
        return $"Command executed: {command}";
    }
}

class NgrokTunnelInfo
{
    public string PublicUrl { get; }

    public NgrokTunnelInfo(string publicUrl)
    {
        PublicUrl = publicUrl;
    }
}
