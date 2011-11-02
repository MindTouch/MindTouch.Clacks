MindToch.Arpysee 0.1
====================
A library for quickly building smtp/memcache protocol style clients and servers

License
=======
Apache 2.0

What's this for?
================
If you want to build a fast RPC server that uses a simple ASCII/Binary protocol with persistent
connections like those used by memcache, smtp, beanstalkd, this is your toolkit. It handles
all the network issues and lets you just define commands and responses you support and expose them
in a client that supports connection pooling.

There is nothing tying the client and server libraries together, so if you wanted to write a client
for some other server using this style of protocol, the client portion will make your life that
much easier.

And if you want to implement the server and let other create clients in other languages, the protocol
is just ASCII and bytes over TCP, so your .NET based server is easily accessed by any other language

So what's this ASCII/Binary protocol?
=====================================
The protcol style pioneered by SMTP and used by such lightweight RPC servers as
memecached and beanstalkd runs over TCP using ASCII encoding. Clients connect,
send commands and data, wait for responses, and close the connection. Commands are
processed serially per connection, allowing a single connection to send/receive many
commands.

The protocol contains two kinds of data: Text lines and byte blobs. Text lines are white-space
separated ASCII encoded commands of the format:

  CMD [ARG1 ARG2 .. ARGn] [BYTECOUNT]\\r\\n

Where CMD is the command name, followed by 0 or more ARGs and optinionally followed by the
number of bytes to expect in a following byte blob. If the command specifies a trailing BYTECOUNT
argument, the server expects the following line to be of the format:

  BYTES\\r\\n

Where BYTES is BYTECOUNT bytes followed by a line terminator.

Responses use the identical format, except that the first element of the Text line is considered
a status code.

Status
======
Early WIP, mostly experimenation

Usage
=====

Both client and server libraries are meant for containment, i.e. the plumbing for your own abstraction of the
protocol rather than raw usage, although there is nothing about the code that enforces such a design decision

Creating a server that can echo arguments
::
    // build server
    var server = ServerBuilder
      .CreateAsync(new IPEndPoint("127.0.0.1", 12345))
      .WithCommand("ECHO")
        .HandledBy((request, response) =>
          response(Response.Create("ECHO").WithArguments(request.Arguments)
        )
        .Register();
      .Build();

    // Run the server until you press enter
    using(server) {
      Console.ReadLine();
    }

Calling the server to echo some arguments
::
    // create a client (uses default connection pool
    using(var client = new ArpyseeClient("127.0.0.1, 12345)) {
      var response = client.Exec(
        Request.Create("ECHO")
          .WithArgument("hi")
          .WithArgument("there")
      );
      Console.Writeline("Server echoed: {0}", string.join(" ", response.Arguments);
    }

Contributors
============
- Arne F. Claassen (sdether)


