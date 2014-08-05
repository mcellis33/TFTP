To run this in Visual Studio, open Tftp.sln and hit f5. I have configured both the server and the test client to run simultaneously while debugging. You should see two console windows pop up. For now, I have sent all of my tracing to these console windows, so check 'em out. Entering any key into one of these windows kills it, as well as stopping the Visual Studio debugger.

The server app sleeps for 500ms before spinning up the server as a basic demonstration of the client timeout/retry feature.

The current client performs this rudimentary test:
1) Delete '<sln directory>\TestOutput.txt' if it exists.
2) Write '<sln directory>\TestInput.txt' to the server (127.0.0.1:69) as 'file0'
3) Read 'file0' from the server into '<sln directory>\TestOutput.txt'.

todo:
- Change Console.WriteLine to txt file trace logging
- create a shim for UdpClient for testing dropped and duplicate packets
- use an actual unit testing framework