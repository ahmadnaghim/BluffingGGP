# General Game Playing With State-Independent Communication - Experimental Setup

This repository contains the experimental setup used to generate our results. It was written in C# and built using Visual Studio. The entrypoint for the program can be found in `ZT23/Program.cs`.

## Raw output

For 1 million simulations per combination of player strategies per game:

```
GoodOrEvilGame

                   Random |       Truthful |       Proposed
Random   |   49.96, 49.99 |   49.92, 49.96 |   49.95, 49.92
Trusting |   50.06, 49.92 |  100.00, 49.99 |  49.97, 100.00
Proposed |   49.99, 24.96 |   74.99, 25.02 |   49.98, 50.04


WeightedGoodOrEvilGame

                   Random |       Truthful |       Proposed
Random   |   50.02, 49.97 |   50.04, 50.01 |   50.06, 49.96
Regular  |    74.93, 0.00 |    74.97, 0.00 |    74.99, 0.00
Trusting |   50.01, 50.00 |  100.00, 25.03 |   50.02, 50.01
Proposed |    74.97, 0.00 |    74.96, 0.00 |    75.07, 0.00


-----------------------------------------------------------

CooperativeSpiesGame

                   Random |       Truthful |       Proposed
Random   |   49.88, 49.88 |   50.05, 50.05 |   49.97, 49.97
Trusting |   50.04, 50.04 | 100.00, 100.00 | 100.00, 100.00
Proposed |   50.06, 50.06 | 100.00, 100.00 | 100.00, 100.00


EnvelopeGame

                   Random |       Truthful |       Proposed
Random   |   49.98, 49.98 |   49.99, 49.99 |   50.06, 50.06
Trusting |   50.06, 50.06 |   65.02, 65.02 |   70.00, 70.00
Proposed |   49.97, 49.97 |   65.02, 65.02 |   70.03, 70.03


-----------------------------------------------------------

ProbableSharedEnvelopeGame

                   Random |       Truthful |       Proposed
Random   |   50.02, 49.99 |   49.98, 50.00 |   50.04, 50.03
Trusting |   50.03, 49.98 |  100.00, 89.98 |  90.02, 100.00
Proposed |   50.02, 50.08 |  100.00, 90.07 |  90.02, 100.00
-----------------------------------------------------------
Random   |          50.03 |          49.99 |          49.97
Trusting |          49.89 |         100.00 |           0.00
Proposed |          50.00 |           0.00 |         100.00


-----------------------------------------------------------

OneShotInvestigationGame

                   Random, Random |       Random, Truthful |       Random, Proposed |     Truthful, Truthful |     Truthful, Proposed |     Proposed, Proposed
Random   |    24.98, 31.31, 31.23 |    25.04, 31.20, 31.28 |    25.05, 31.29, 31.32 |    24.98, 31.27, 31.23 |    24.94, 31.15, 31.25 |    24.95, 31.24, 31.19
Trusting |    25.01, 31.15, 31.25 |    43.72, 35.95, 35.89 |    31.24, 35.86, 48.47 |   100.00, 49.99, 50.08 |    62.53, 37.58, 68.65 |    37.52, 68.74, 68.69
Proposed |    25.02, 19.24, 19.29 |    54.27, 24.00, 28.63 |    38.52, 19.85, 44.87 |    87.51, 37.51, 37.52 |    75.03, 25.02, 49.98 |    49.93, 50.04, 50.07

```
