# Real Time [![Steam Downloads](https://img.shields.io/steam/downloads/1420955187.svg?label=Steam%20downloads&logo=steam)](https://steamcommunity.com/sharedfiles/filedetails/?id=1420955187)

A mod for the Cities: Skylines game. Adjusts the time flow in the game to make it more real.
**Real Time** makes the game way more real and more challenging!

## Key Features
### Time flow

- The game time flows slowly. The time speed can be configured for the daytime and for the night time separately.
- The sunrise and the sunset times depend on the map location and on the day of year.
- Citizens grow up slower. 1 in-game day equals to 1 citizen's year. Citizens live up to 85 years, so with Real Time - up to 85 in-game days.
- Slower aging changes the education system: a child needs 5 in-game days to graduate from an elementary school and become a teen. A teen needs 10 in-game days to graduate from a high school and become a young adult. Finally, a student needs another 5 in-game days to get the highest education level.

### Work...

- The adult citizens go to work in the morning. The children go to school.
- There are weekends. No school, no usual work on weekends!
- The adults may also work second shift, night shift, and some of them work on weekends!
- A lot of traffic, especially at rush hour times.
- Citizens can go out for lunch, if there are some commercial buildings near the citizens' workplaces.
- Citizen can go on vacation for some days. Families prefer to go on vacation all together (parents and children).

### ...and relax

- The school ends earlier, so the children can have spare time.
- After work or school, the citizens go shopping or relaxing.
- Children stay at home in the late evening.
- Citizens can attend events like football matches. In addition to the game events, there are some other events for various unique buildings in the city.
- Tourists can stay in the city for longer, if there are some hotels.
- In the night time, no one will visit parks; a single exception: the 'Night tours' policy is activate in a park.
- Only few citizens will go out and party at night (in leisure buildings, if there are any). 
- Some adult citizens will take night shopping tours.

### More real

- Citizens might go shopping even when they don't need any goods - just for fun.
- Tourists also prefer to sleep at night.
- The buildings will be constructed slowly.
- There are restrictions how many construction sites are allowed at the same time in the city.
- The building construction sites pause at night.
- Citizens switch the lights off when they are going to sleep, as well as many other parks and buildings.
- When the weather becomes bad, citizens try to shelter from the weather in the buildings.
- Citizens remember how long they need to get to work and use this in their schedules, trying to be on-time.
- When waiting for public transport for too long or stuck in a traffic jam, citizens get angry and cancel their journeys.

## Performance note
**Real Time** works best with medium-sized cities (population up to 65.500). With large cities, there are some game limitations that make it difficult for **Real Time** to keep the citizens behavior realistic. Furthermore, the CPU usage and the graphic adapter load increase drastically, because every citizen needs to be precisely simulated.

## Distribution
**Real Time** is published on Steam Workshop. To use **Real Time**, players need to subscribe to [this Steam Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3059406297).

###❗[h2][b] Important: Version 2.7 is not backward compatible for saving. If you save your game under 2.7 and later try to load it using version 2.6, all citizen schedules will be lost. Please save your game in a NEW slot to preserve a backup of your 2.6 playthrough.

## Update 2.7
#### General
* Cims can now choose to have breakfast, lunch, and supper throughout the day.  
* Commercial buildings can now be configured as any combination of:  
   o Food  
   o Shopping  
   o Entertainment  
* Garbage, mail, and crime accumulation rates are now configurable per building.  
* Streamlined and reorganized options menus for easier configuration.  

#### Race DLC Enhancements
* Improved event scheduler with support for:  
   o Custom event start day, month, hour, and minute  
   o Daily, weekly, or single-event scheduling  
   o Configurable event preparation time  
* RaceHQ maximum race duration increased to 25 laps.  
* Maximum ticket price capped to 100.  
* Visitor attendance probability is now influenced by ticket price.  
* Visitors will prefer to remain at the event until conclusion 

#### New Info Views & Tools
* From Info Views → Population, select a building to highlight related Cims throughout the city. 
  Color Indicators:
    o Green — Residents  
    o Blue — Workers  
    o Magenta — Visitors  
   o Yellow — Students  
 This feature makes it easy to visualize how Cims interact with buildings and destinations. For 
 example, selecting a transit station and then a park or monument can reveal citizens traveling to 
 that location.
* Reset Building Garbage Buffer via the tools in the options menu.

## Update 2.6
#### School and Student-Related Features
* Students and workers at campus will try to eat at the cafeteria for lunch.
* Schools can now recruit students even when closed.
* The academic year starts on weekdays between 9 and 10 AM.
* The academic year includes a 24-hour gap before the next year for graduation ceremonies and to let students finish and graduate.
* The academic year will not end at night or on weekends.
* Students, like workers, won’t go to school if half the class has already passed.
* "Toga party" duration is now adjustable by users.

#### Hotel and Tourist Features
* Hotels are now searched based on a list of availability, not based on a search radius.
* Support for base hotels and After Dark workshop growable hotels to accommodate tourists.
* Commercial and office buildings will still emit hotel attractiveness even when closed.
* Tourists will leave the city if there are no suitable alternatives when buildings close.

#### City Views
* Wind mode will show open and closed buildings.
* Natural Resources mode will display people in closed buildings in blue.

#### Building and Gameplay Mechanics
* People will start searching for a new place to visit when a building is about to close. If a building has already closed, they will go home, to a hotel, or leave the city.
* Added new events in unique buildings from various DLCs.
* Fix for the bug where people were stuck waiting for the hospital.
* Added "ignore policy" functionality—currently works only for leisure buildings to bypass the "nimby policy."
* Fish market and library work-time support.
* Go on vacation and return between midnight and 2 AM.

## Update 2.5
* Academic year length support fix.
* Opreation hours for almost all building types.
* Night class quota bug fix that made people choos night class over day class.
* When a building is not working it will not send/request vehicles and will not report problems.
* Buildings will still show attractivness even when closed.
* Car parking buildings are always open and removed attractivnes and visit.
* Cafeteria and Gym will get visited by students of the same campus to eat lunch or entertiament and will also have visitiors count.
* Clear building fire manager for players that have loading issues.
* Show citizen current state when applicable.
* First time workers that get to work early will try to visit a nearby shop until work begins or go home if not visit place found (3times).
* If you have combined ai installed, citizen will visit a bank or a post office. To control the percentage please use the combined ai mod sliders.
* Fix global and type settings saving and loading.
* Lock/Unlock building settings button - to avoid type settings to be applied.
* Support for UniversityHospitalAI from combined ai mod.
* Citizens will leave buildings when they become closed.
* Citizen can schedule shopping or breakfast before first shift work if they have enough time.

## Update 2.2.1
* Fixed incorrect work hours for buildings and new workers assigned to the wrong shift.
* Added support to Combined AI's mod allowing you to visit banks and post offices.
