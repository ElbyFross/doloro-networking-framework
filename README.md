# Doloro Networking Framework
It's the framework that provide fast and easy way for creation of client and server applications powered by .Net Framework.

## Documetation
| Format | Description | Link |
| :---: | --- | :---: |
| Wiki | Contains a description of logic processes into the framework. | [Link](https://github.com/ElbyFross/doloro-networking-framework/wiki) |
| API | An online documentation with API overview. | [Link](https://elbyfross.github.io/dnf-docs/) |
| Local | A repository with generated documentation as could be used offline. | [Link](https://github.com/ElbyFross/dnf-docs) |

## Support
The **DNF** is an open source project and it's require your support. 

The less but still very important that you could to do is considering of setting a **Star** and starting following the project. Your activity and interest can be used as proof of product relevance during negotiation with big sponsors. Thank you in advance!

If you want to make a bigger contribution you may find other options and goals description by the following [link](https://github.com/ElbyFross/doloro-networking-framework/wiki/Support-of-the-initiative).

# Scalability
### Routing tables
Control routing between servers using easy to use [Routing tables](https://github.com/ElbyFross/doloro-networking-framework/wiki/RoutingTable) those include built-in filtering features provided with easy to understand regular expressions.

### Isolated high end logic 
Create your own high-end server logic to control complex custom network without demands to deep knowledge of networking backend.

### Modifiable pipeline of networking logic
Framework provides a list of ready to use handlers that fully control transmission, but you always able to create your own networking logic on every step to covering your own purposes.

`(Server\Clent loops => Connection handler => Transmisssion handlers => Data handlers)`

# Security
### Encryption operators
The framework has a realy flexible encryption archtecture that allow you to implement your own encryption algorithms. 

### Named Pipes
Transmission powered by named pipes that significant improve security on NT systems.

### Authority Controller
An addon that provides possibility to create users, control them's rights. Implements tokens based rights control system.

Contains built-in queries:
- Logon
- Logoff
- Set token rights
- Get guest token
- New user
- New user password
- User ban

# Simplicity
### Auto encryption
[Article](https://github.com/ElbyFross/doloro-networking-framework/wiki/Encryption) **|** [Source](./Core/PipesProvider/Security/)

A client and server are able to automaticly exchange with public asymmetric keys to each other. During using standard handler all what you need it's just configurate and encryption params at your [RoutingTable](https://github.com/ElbyFross/doloro-networking-framework/wiki/RoutingTable) and the line transmission will auto encrypted.

### Auto control of transmission
Built-in networking handlers control logic loop of transmission without your involving.
1. Create query
2. Open `TransmisssionLine`
3. Enque query to transmission line.
4. Recive answer to your data handler in case of duplex query.

**See also:** [Example]() | [Wiki:Query](https://github.com/ElbyFross/doloro-networking-framework/wiki/Query) | [Wiki:TransmissionLine](https://github.com/ElbyFross/doloro-networking-framework/wiki/Transmission-controllers)

### Ready to use servers
[Source](./Examples/Servers/)

- [Session provider](https://github.com/ElbyFross/doloro-networking-framework/wiki/SessionProvider) - provide tokens, control users profiles, provide complex hierarchy.
- [Queries server](https://github.com/ElbyFross/doloro-networking-framework/wiki/QueriesServer) - relay server that would receive queries and redirect to target servers by using `Routing tables`.
  
### LSA configurator (Experemental)
[Article](https://github.com/ElbyFross/doloro-networking-framework/wiki/General-security#lsa-modification) **|** [Source](./Core/PipesProvider/Security/)

Allows to configure your OS in one click relative to required rights.

### Server's built-in transmission algorithms
[Article](https://github.com/ElbyFross/doloro-networking-framework/wiki/Transmission-controllers) **|** [Source](./Core/PipesProvider/Server/TransmisssionControllers/)

- Client to Server
- Server to Client
- Broadcast

### Client's built-in transmission algorithms 
[Article](https://github.com/ElbyFross/doloro-networking-framework/wiki/TransmissionLine) **|** [Source](./Core/UniformClient/Providers/PipesProvider/BaseClientPPHandlers.cs)

- Duplex query
- Input query
- Output query
- Receiving broadcast message
