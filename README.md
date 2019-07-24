# Doloro Networking Framework
It's the framework that provide fast and easy way for creation of client and server applications powered by .Net Framework.

# Scalability
### Routing tables
Control routing between servers using easy to use `Routing tables` that include built-in filtering powered by easy to understand regular expressions.

### Isolated high end logic 
Create your own high-end server logic to control complex custom network without demand to deep knowledge of networking backend.

### Modifiable pipeline of networking logic
Framwork provides a list of ready to use handlers that fully controll transmisssion, but you always able to create your own networking logic on every step to covering your own purposes. 

`(Server\Clent loops => Connection handler => Transmisssion handlers => Data handlers)`

# Security
### RSA encryption of transmission
Framework provide full API and automatic controllers for providing secure transmission of any your message. That simplify using and modification of system.

### Named Pipes
Transmission powered by named pipes that significant improve security on NT systems.

### Authority Controller
Addon that provides possibility to create users, control them's rights. Implements tokens based rights control.

Contains built-in queries:
- Logon
- Logoff
- Set token rights
- Get guest token
- New user
- New user password
- User ban

# Simplicity
### Auto RSA encryption
[Dirctory](./Core/PipesProvider/Security/)

Client able automaticly recive a public RSA's key from server and encode message beffore transmisssion. 
Append your client's RSA's public key in `pk` propery in query and server would auto encrypt answer.

Just don't forget to set `RSAEncryption` field to `true` in client's `RoutingTable`'s `Instruction`.

### Auto control of transmission
Built-in networking handlers control logic loop of transmission without your involving.
1. Create query
2. Open `TransmisssionLine`
3. Enque query to transmission line.
4. Recive answer to your data handler in case of duplex query.

### Ready to use servers
[Dirctory](./Examples/Servers/)
- Session provider - provide tokens, control users profiles, provide complex hierarchy.
- Queries server - relay server that would receive queries and redirect to target servers by using `Routing tables`.
  
### LSA configurator
[Dirctory](./Core/PipesProvider/Security/)

Allows to configure your OS in one click relative to required rights.

### Server's built-in transmission algorithms
[Dirctory](./Core/PipesProvider/Server/TransmisssionControllers/)

- Client to Server
- Server to Client
- Broadcasting

### Client's built-in transmission algorithms 
[Dirctory](./Core/UniformClient/Providers/PipesProvider/BaseClientPPHandlers.cs)

- Duplex query
- Input query
- Output query
- Receiving broadcast message
