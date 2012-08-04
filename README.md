Flux
====
A lightweight [OWIN](http://owin.org) web server for .NET and Mono

## Why?
Because there aren't enough lightweight OWIN web servers for .NET and Mono. Sometimes you just don't want to run everything on top of IIS or XSP. Sometimes you just want a command-line app that does the job for you. Hence Flux.

## Usage
You can use Flux as a command-line executable. Just `cd` to the directory with your application assemblies in it and run `flux`. By default, Flux runs on port 3333. If you want, you can specify a port number:

`flux 80`

You can also reference Flux from your own executable application and start it internally. Take a look at [Program.cs](https://github.com/markrendle/Flux/blob/master/Flux/Program.cs) in the source to see how that works.

## Technical stuff
Flux uses [TcpListener](http://msdn.microsoft.com/en-us/library/system.net.sockets.tcplistener.aspx) and does its best to handle headers and such itself. It doesn't use [HttpListener](http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx) because that requires elevated privileges, Eris knows why.

Flux also makes use of [Fix](http://github.com/markrendle/Fix), which is "glue" for OWIN. Fix works by finding methods in code that have been marked with a [MEF](http://mef.codeplex.com/) [Export](http://msdn.microsoft.com/en-us/library/system.componentmodel.composition.exportattribute.aspx) attribute with the contract name "Owin.Application".

Finally, Flux works natively with [Simple.Web](http://github.com/markrendle/Simple.Web), as you can see in [this test project](https://github.com/markrendle/Flux/tree/master/Flux.Test.SimpleWeb).

### OWIN?
Fix and Flux use the OWIN standard for interoperability between Web Servers and Web Frameworks or Applications, without taking a dependency on the *optional* Owin assembly.
More information about OWIN is available [here](http://owin.org).

## Caveat Utilitor
Flux is very much under development, and only intended for development use. Right now, what error handling exists is unsophisticated. There's a whole bunch of HTTP stuff that isn't implemented, such as the Connection: keep-alive header. It certainly won't handle SSL, but proxies like nginx will take care of that kind of thing. Most importantly, it hasn't been optimized or checked for memory leaks and the like yet.

As at the time of this writing, I haven't tested Flux with Mono.

## Contribute
Pull requests are very much accepted, so if you can either fix a problem or add a cool feature please do. If you find a problem that you can't (or just don't want to) fix, please raise an issue.