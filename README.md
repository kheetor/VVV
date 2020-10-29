# VVV
VVV - Community Sandbox game
A Unity game with LLAPI networking, dedicated server - client model, x86 and HTML5 WebGL releases

To try this game
1. go to vvv.vectorama.info
2. type in some username and password (remember them if you want to return) and hit "Create this account"
3. Play the game using WASD keys. Camera controls are like in WoW - drag with M1 to turn camera and drag with M2 to also align the player. You can create and stretch and color some primitives using the UI. If you get stuck in geometry you can enable Ghost mode.
4. To unlock fun driving mechanics build a "base" first using the UI button, then select base and click on the "Research racing drone"
5. You can switch between floating and driving drone with V key.
6. Resource objects can be found further away on the map, you can pick them up using Q and throw by holding E and releasing when you like the trajectory

# Features:
- Players can create primitives like cube and sphere to build whatever they like. The objects can be moved, colored and stretched by the player who created them.
- Cube primitives are using a 6-way splitting I invented. It's like what you do in Unity for sprites but in 3D: you can specify dedicated corner, edge and face pieces for the object. When the object is scaled, the parts are stretched individually with protected aspect ratios instead of simply scaling the entire mesh uniformly.
- Released clients are x86 and HTML5. Android / iOS would be nice fit as well but building touchscreen controls for movement takes a lot of time.

# Game design
The game design such as progression and resource management is little rudimentary and experimental. But there are 2 drones that you can use to collect resources and take them back to your base by throwing or using conveyors. (Conveyor building is currently under construction but you can try the existing ones).

# Networking
- LLAPI networking using channels and send and receive, JSON serialization
- user authentication with basic username / password credentials, with passwords only hash is sent, stored and compared. Note this project doesn't support SSL because builtin Unity networking simply doesn't allow it.
- Network traffic is culled per player, you only receive objects by proximity. This is a feature I want to test and refine more when I find some playtesters.
- Dedicated x86 server that handles authority over most actions. Since this is a non-competitive sandbox game, the server doesn't auth player movement speed or collision. Some limitations like max distance for picking up resources and calculating trajectories are only client-side checked.
