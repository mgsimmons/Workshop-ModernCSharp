# Workshop-ModernCSharp
*This repository contains the code for a collection of lab exercises to go along with my one-day Modern C# hands-on workshop.*

If you have not done so already, you can either fork this repository to your own GitHub account, so you can commit files to it, or you can clone it and work on it locally (though you might have to `git stash` your changes if you want to switch between branches of the project).

## Overview
> *NOTE: Throughout this lab manual you will see these little asides from me to you. They're intended to help you understand what I'm thinking and what the lab is trying to help you practice. Keep in mind, too, that while the whole purpose of these labs is to give you some directed path to help you practice the C# syntax, you are a grown adult and this isn't school. If you learn by exploring ideas on your own, please feel free. [^1]*

Welcome! You have just been recruited to the ***World's Worst SuperHero Agency***, a burgeoning startup looking to create accountability for the world's worst superheroes.

> *Yeah, I don't believe in boring labs.*

We have received a grant from the President-Elect Norman Osborn to create an online HTTP API for tracking incidents, the heroes assigned to those incidents, and the damage done during those incidents. As the developer on the team, it's your job to create the system, and as President Osborn is good friends with Bill Gates, Satya Nadella, and the other billionaires of Microsoft, he has wisely decided to use C#/.NET for the project. And, of course, nothing done in a Osborn administration is ever done using legacy tools or products--only the best for Mr Osborn--so all of the latest features of C# are available to you.

> *Apologies if you are more of a DC fan than Marvel. I collected X-Men comics in high school, and played the Marvel SuperHeroes RPG in college. And hey, now that the Marvel Cinematic Universe has been a part of our collective cultural experience for 20 years, it feels pretty safe to assume you'll know who at least a few of those folks are. And who doesn't love a superhero action movie?*

Of course, normally, we would hand this kind of task off to J.A.R.V.I.S. or F.R.I.D.A.Y. or even E.D.I.T.H, but all of them seem to be offline for reasons Mr Osborn's team cannot explain. "National security!", they keep saying, so it must be important!

> *For those wondering what the various Marvel AIs are up to, I refer you to the most recent episodes of "VisionQuest". Pretty good stuff.*

We don't need them, though, we have you!

The project itself is broken into three parts: a domain object layer, the persistence layer (which will be done using a lightweight CQRS approach, since that's way more modern than that legacy "CRUD" thing), and the HTTP API layer that calls into both. Are you ready?

## Lab 0: Testing the waters
*(This lab is estimated to take 15 mins.)*

First off, being a new hire, you need to test your laptop setup to make sure that all the tools are in place.

> *As our cheerful team lead has said, this lab is intended purely to test your setup and make sure the CLI tools work. If you prefer to use Visual Studio, you are free to do so, but part of the "modern" C# experience is the use of the CLI toolset. Visual Studio exposes some of this functionality, but it's scattered around the GUI. Once the project is generated, you should be able to load and run the project in VS without issue, if you feel more comfortable in that environment.*

### Hello world!
Fire up a Terminal prompt, and create a new folder, `Hello`. Within that folder, execute the following commands:

```
dotnet new console
dotnet run
```

You should see the traditional "Hello World!" message. If you don't, you may have a problem. If you do, great! Fire up a text editor (President Osborn of course recommends "GoblinCode", but most of us here use VSCode) and edit the message in the `Program.cs` file to read something longer, like "Hello World! This is a test of the WWSHA emergency broadcast system."

### Hello, Again!
Next, let's test that you can see some of the new .NET CLI functionality; create a new folder (a peer to the `Hello` folder), call it `HelloAgain`, and within it, try this:

* Look at the list of installed `dotnet` templates for the `Void.Game` templates. (Chances are very strong they won't be there.) If they aren't installed, install them.
* Create a new `voidgame` project.
* Run the project.
* You should see a window with a "cornfield blue" background appear on your screen after a few seconds.

If you got both projects to run without issue, great! If you had a problem, please don't hesitate to contact us.

### Bonus Level: Going Native
Try converting one of the above over to use "[AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)" compilation into a native binary. This will, however, require additional compiler toolchains on your machine, and will fail if you don't have them:

* **Windows:** Install Visual Studio 2022 (or newer) with the Desktop development with C++ workload. Alternatively, as noted by developers on Stack Overflow, you can install the standalone Visual Studio Build Tools with the C++ x86/x64 tools and Windows SDK components.
* **Linux:** Install clang and the developer packages for zlib (and libicu unless running in globalization-invariant mode).
* **macOS:** Install the latest Command Line Tools for Xcode.

Assuming those are present, converting the project to use AOT as as simple as adding `<PublishAot>true</PublishAot>` inside a `<PropertyGroup>` element in your `.csproj` file.

In the meantime, your team leader will now begin speaking about the latest features of C# so you can be ready for what comes next!

## Lab 1: Domain model
Welcome back! Now that you are refreshed on some of the core syntax of the language, we can proceed by building the core domain model as a .NET Class Library for use by our project. We build it as a Class Library, of course, so that it could be reused in other projects, such as the rumored "Mutant Registration Act" discussed among certain members of the President's administration.

Before you get too far, create a new directory (we suggest `WWSHA.Domain`) for the new Class Library project.

Our domain model is pretty straightforward:

* A `Hero` has a name, a superpower, their current availability status ("Available" or "Unavailable"), a reputation (just a simple integer score), and a list of Assessments (their personal superhero history). Note that we also deliberately choose the misspelling of `Heros` instead of heroes, just to keep pluralization rules simple.

* An `Incident` is a location (city name), a time (local time), descriptive text, and a category. The category can be one of "PettyCrime", "AlienInvasion", "SupervillainAppearance", "DisagreementBetweenHeros", "DrunkenBarFight", or "WanderingKaiju". The Incident is either "Opened" (before it has a Hero assigned), "InProgress" (after it has a Hero), or "Closed".

* An `Assignment` is created when a Hero is assigned to an Incident. Up to three Heros can be added to the Assignment.  While a Hero is Assigned, they cannot be assigned to a new Assignment.

* A `Report` is when a city official or one of the city's fine upstanding citizens phones in to our agency to report damage caused by a superhero during an Incident. This is also sometimes called a "damage report", but we go with the term "report" because it's shorter and sometimes there isn't any damage. Any Reports filed while the Incident is open are added to the Assignment's damage history.

* An `Assessment` is the agency's final assessment of the Hero's conduct during the Incident. The Hero's reputation goes up by 5, minus the number of DamageReports filed during that Incident.

Business Rules:

* Heros are registered with the agency and made available for assignments.
* When an Incident is filed with the agency, an available Hero is Assigned to that Incident. The Incident becomes InProgress, and the Hero is unavailable until the Incident is closed and an Assessment is added.
* Up to three Heros can be assigned to an Incident. If no Heros are available, the Incident remains Opened until a Hero is available.
* While the Incident is Opened, reports of collateral damage from that city are added to the Assignment history.

> *The above is intended to give you a general overview of the domain types and business rules for the project, some of which were created just to make things interesting. You are free, of course, to modify or simplify any of it as you wish if you find yourself frustrated or drowning in the minutiae.*

For example, if ***Captain Inconvenient*** (*Superpower:* teleporting people one to three feet to the left of their current location, *Reputation:* 1, no assessments yet) is wandering around Seattle, WA, looking for work, that means they are available for assignment. Suddenly, an upstanding citizen reports an Incident: while visiting Redmond, WA (a suburb of Seattle) at noon, they spotted "What looked like a bunch of rowdy sportsball fans getting into a drunken bar brawl! (Something about 'Seahawks suck this year' or something?)", which means an Incident with status Opened is created. Fortunately, Captain Inconvenient can be there in 30 mins (congestion across the 520 bridge is painful, even for a teleporter), and they can be assigned to the Incident.

When they arrive, sure enough, there's a group of three people wearing T-shirts proclaiming "Let's go Patriots" and they're obviously drunk on Boston lagers, stumbling around, yelling something about the "cah" and "warsh", and knocking tables over. Clearly this is a job that needs Captain Inconvenient! He immediately swoops into action, teleporting them all around the room until, dizzy, they grow dizzy, fall over, and immediately pass out. Assignment complete!

Unfortunately, while he was teleporting them around, several tables' legs were busted off, and about a dozen bar glasses are broken, so the bar owner files a Report. Still, one damage report isn't bad, and Captain Inconvenient's reputation goes up by 4. Not like that time that ***The Monocle*** (*superpower:* make anyone fall asleep with his monocle if he can make eye-to-eye contact with them) tried to take on the kaiju named The Creature with a Million Eyes in Sheboygan....

Your task, of course, is to create all of these classes and other types, including any exception types that would need to be thrown from various places to indicate problematic attempts (like assigning a Hero when they're already out on assignment!). All of these are just domain types, however--no persistence, storage, or HTTP API. That comes in later steps.

And, yes, I suppose, if you feel like you have the time, create a unit-test project to test all this, but really, you're a professional, you don't really need that, do you?

> *Between you and me, yes, yes you do. I would, anyway. But everybody works differently, and if you're not familiar with any of the .NET unit-test frameworks, don't lose any sleep over it. If you do, however, want to write some tests against your domain model, creating a new test project is either one of `dotnet new nunit` (NUnit), `dotnet new mstest` (MSTest), or `dotnet new xunit` or `dotnet new xunit3` (xUnit). From there, you just need to reference the Class Library project (`dotnet reference add ../WWSHA.Domain/WWSHA.Domain.csproj`, if the domain library is in a directory called `WWSHA.Domain`) and start writing your tests.*

## Lab 2: Persistence (Storage and Access)
In this section, we're going to implement two interfaces to represent the "query" and "command" side of a persistence layer. Everything will be in-memory, 

## Lab 3: HTTP API


## To Be Continued...
There's certainly more work that could be done from here:

* wire it up to an actual database, instead of the in-memory storage engine

    * [SQLite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
    * [PostgreSql](https://www.npgsql.org/) or a cloud-hosted variation like [Supabase](https://supabase.com/docs/reference/csharp/introduction)
    * [Azure CosmosDB](https://azure.microsoft.com/en-us/products/cosmos-db)
    * MongoDB or another document database

    ... or maybe consider going in an entirely different direction with an event-sourcing database.

* create a web front-end for it *(being careful not to violate any copyright restrictions, of course, lest you anger the lawyers from the House of Mouse....)* in your favorite client-side or server-side web framework.

* create a mobile app client for it (perhaps using [MAUI](https://dotnet.microsoft.com/en-us/apps/maui), [Flutter](https://flutter.dev/) or [React Native](https://reactnative.dev/)?)

* create a command-line (or even Terminal UI [^2] ) client for it

---

[^1]: One of the first projects I built back when I was first exploring C# was to build a "command-line type explorer", which took an assembly and a type name at the command line, loaded that assembly, and then used System.Reflection to walk the contents, just to see what compiler magic was being used. Today, I'd do that using a library like [Cecil](https://github.com/jbevain/cecil) to do an even lower level of examination.

[^2]: I continue to be fascinated by how much functionality can be expressed in a terminal window via libraries like Terminal.UI. Of course, I was a huge fan of [TurboVision](https://github.com/magiblot/tvision) back in the late DOS era, so maybe ignore my opinions on the subject....
