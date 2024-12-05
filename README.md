[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

<!-- HEADER -->
<div align="center">
<h1>Master-Server-Unity</h1>
    <a href="https://github.com/Assambra">
        <img src="Github/Images/Assambra-Logo-512x512.png" alt="Assambra Logo" width="80" height="80">
    </a>
</div>

<!-- LINKS -->
<div align="center">
    <p align="center">
        <a href="https://github.com/Assambra/Master-Server-Unity/wiki"><strong>Explore the docs »</strong></a>
    </p>
    <p align="center">
        <a href="https://github.com/Assambra/Master-Server-Unity/releases">Get Latest</a>
        ·
        <a href="https://github.com/Assambra/Master-Server-Unity/issues">Request Feature</a>
        ·
        <a href="https://github.com/Assambra/Master-Server-Unity/issues">Report Bug</a>
    </p>
</div>

<!-- DEMO IMAGE -->
![Unity-Master-Server][product-image]

<a name="readme-top"></a>

<!-- TABLE OF CONTENTS -->
## Table of Contents
<ul>
    <li><a href="#key-features">Key Features</a></li>
    <li><a href="#project-structure">Project Structure</a></li>
    <li><a href="#project-setup">Project Setup</a></li>
    <li><a href="#master-server">Master-Server</a>
        <ul>
            <li><a href="#setup-database">Setup Database</a></li>
        </ul>
    </li>
    <li><a href="#unity-server">Unity-Server</a>
        <ul>
            <li><a href="#create-the-unity-server">Create the Unity Server</a></li>
            <li><a href="#setup-server-path">Setup Server Path</a></li>
        </ul>
    </li>
    <li><a href="#unity-client">Unity-Client</a></li>
    <li><a href="#contact">Contact</a></li>
</ul>

<!-- KEY FEATURES -->
## Key Features
<ul>
    <li>Extending EzyFox Server's room management to launch a dedicated Unity server</li>
    <li>One Unity-Server for all rooms/servers</li>
    <li>Area of interest System on the Unity-Server</li>
        <ul>
            <li>Capability to spawn or despawn entities based on range</li>
            <li>Each entity has a NearbyPlayer list and sends position updates only to players in this list</li>
        </ul>
    <li>Entity-based System</li>
    <ul>
        <li>An entity is the base and can be a player, effect, or building (currently, only Player is implemented)</li>
        <li>Each entity automatically adds the NetworkTransform component, is added to the client’s entity list, and receives position updates or spawn/despawn events based on range</li>
    </ul>
</ul>

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- PROJECT SETUP-->
## Project Setup

Clone the repository:
```bash
git clone git@github.com:Assambra/Master-Server-Unity.git
```
Download and extract the file: [ezyfox-server-csharp-client-1.1.6-unity.zip](https://github.com/youngmonkeys/ezyfox-server-csharp-client/archive/refs/tags/v1.1.6-unity.zip).

Perform the following steps for both Unity projects: **Unity-Client** and **Unity-Server**:
1. Insert the extracted folder `ezyfox-server-csharp-client-1.1.6-unity` into the root folder `/Assets` of your opened Unity project.
2. Open the Unity Editor and navigate to the folder `Assets/ezyfox-server-csharp-client-1.1.6-unity`.
3. Right-click inside the folder -> **Create** -> **Assembly Definition**, and rename it to `com.tvd12.ezyfoxserver.client`.

#### Unity-Server Setup
In the Unity Editor:
1. Navigate to `Assets/Unity-Server/Scripts/Assambra.Server`.
2. In the Inspector, locate the **Assembly Definition Reference** section.
3. Remove the missing reference starting with `GUID:` (if present).
4. Add a new reference by clicking the plus sign (+) and select the earlier created `com.tvd12.ezyfoxserver.client` assembly definition.
5. Click **Apply**.

#### Unity-Client Setup
Repeat the same steps for the **Unity-Client** project:
1. Navigate to `Assets/Unity-Client/Scripts/Assambra.Client`.
2. Follow the same process for updating the **Assembly Definition Reference**.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- PROJECT STRUCTURE-->
## Project Structure
A brief overview of the structure of the project. We use three projects: master-server (EzyFox Server), Unity-Server, and Unity-Client. It is important to know that the Unity-Servers are also clients for the master-server, and we always use the Ezyfox Client SDK for communication between Unity-Client <--> master-server and master-server <--> Unity-Server.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- MASTER-SERVER -->
## Master-Server
We use an EzyFox server that acts as a master server. Its tasks are user management, processing database requests, and starting and managing a Unity server for each room. The client makes a request to the master server, which forwards the request to the Unity-Server. Once these requests have been processed by the Unity server, they are sent to the master server and from there sent back to the client(s).

<!-- SETUP DATABASE -->
### Setup Database

1. Install mongoDB.
2. Open your mongosh.
3. Create your Database:

```bash
use master-server
```
4. Create a new user and password and give it access to the created database:
```bash
db.createUser({user: "root", pwd: "123456", roles: [{role: "readWrite", db:"master-server" }] })
```
5. Create the following collections:
```bash
db.createCollection("user", { collation: { locale: 'en_US', strength: 2 } })
```
```bash
db.user.createIndex({ username: 1 })
```
```bash
db.createCollection("character", { collation: { locale: 'en_US', strength: 2 } })
```
```bash
db.character.createIndex({ name: 1 })
```
```bash
db.createCollection("character_location", { collation: { locale: 'en_US', strength: 2 } })
```

6. Use this file for the next step:

Location: `master-server/master-server-common/src/main/resources/master-server-common-config.properties`

7. Insert the following values for your database and change them to your needs:
```bash
database.mongo.uri=mongodb://root:123456@127.0.0.1:27017/master-server
database.mongo.database=master-server
database.mongo.collection.naming.case=UNDERSCORE
database.mongo.collection.naming.ignored_suffix=Entity
```
In this example file, we use:

- user: root
- password: 123456
- database: master-server

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- UNITY SERVER -->
## Unity-Server
We use a single project for the Unity Servers. The server contains all server scenes that correspond to a room. When it starts, the server receives three parameters: username, password, and room. The server then uses our Module-GameManager to start the corresponding server scene. This has the advantage that, on the one hand, we only use one project and do not have to create another server project for each server and can therefore share the code that is the same for all servers. The Module GameManager uses a persistent scene where all manager classes/GameObjects are located. The room scene with the 3D objects such as terrain are additionally loaded.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CREATE THE UNITY SERVER -->
### Create the Unity Server
Open the Unity-Server project in Unity. Open Build Settings: File -> Build Settings. Be sure to include all server scenes in "Scenes In Build"; if not, add them. Select as Platform Dedicated Server and choose under Target Platform your platform, Windows or Linux, and build the project.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- SETUP SERVER PATH -->
### Setup Server Path
Open the master-server project in your IDE, edit the file: `master-server/master-server-app-api/resources/application.properties` and add the path to your server executables from the previous step where you saved them. E.g., `D:/Game Builds/Unity-Server/Unity-Server.exe`.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- UNITY CLIENT -->
## Unity-Client
Open the Unity-Client project in Unity and use the Persistent Unity Scene location `Assets/Client/Scenes/Persistent`. If the master-server is running, you can click Play in the Unity Editor.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTACT -->
## Contact
Join us on <a href="https://discord.gg/vjPWk5FSYj">Discord</a>

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- DOCUMENT VARIABLE-->
[contributors-shield]: https://img.shields.io/github/contributors/Assambra/Master-Server-Unity.svg?style=for-the-badge
[contributors-url]: https://github.com/Assambra/Master-Server-Unity/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Assambra/Master-Server-Unity.svg?style=for-the-badge
[forks-url]: https://github.com/Assambra/Master-Server-Unity/network/members
[stars-shield]: https://img.shields.io/github/stars/Assambra/Master-Server-Unity.svg?style=for-the-badge
[stars-url]: https://github.com/Assambra/Master-Server-Unity/stargazers
[issues-shield]: https://img.shields.io/github/issues/Assambra/Master-Server-Unity.svg?style=for-the-badge
[issues-url]: https://github.com/Assambra/Master-Server-Unity/issues
[license-shield]: https://img.shields.io/github/license/Assambra/Master-Server-Unity.svg?style=for-the-badge
[license-url]: https://github.com/Assambra/Master-Server-Unity/blob/main/LICENSE
[product-image]: Github/Images/Master-Server-Unity.jpg
[Unity-url]: https://www.unity.com
