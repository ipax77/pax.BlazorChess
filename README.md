# Introduction

Blazor Server Electron app for analyzing chess games with UCI chess engines on local hardware

!Beta version!

I got a new computer with 16 CPU cores and needed something to test the new toy. I found no decent GUI for this so I ended up writing my own ..

![sample pic](/images/review.png)

# Getting started
## Prerequisites
* dotnet 6
* Download UCI Chess Engine(s) (e.g [Stockfish](https://github.com/official-stockfish/Stockfish/releases/tag/sf_14.1) or [Lc0](https://github.com/LeelaChessZero/lc0/releases)) and add them to the configuration (make sure you have the correct engine version for your CPU/graphic card)

## Dependencies
* [pax.chess](https://www.nuget.org/packages/pax.chess)
* [pax.uciChessEngine](https://www.nuget.org/packages/pax.uciChessEngine)

# Known Limitations
* No Chess960 support
* At the moment only uncommented PGN imports are supported
* No sub variations from engine pvs
* No sub variations in the database
* FEN import not tested

(Debug logging enabled)