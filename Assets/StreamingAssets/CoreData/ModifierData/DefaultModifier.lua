﻿Type = "Modifier"

Name = "Default"
TargetType = "Game"
IsTileLimited = false
IsPlayerExclusive = false

AdditionalDesc = "DefaultDesc"

Scope = {}
Scope.Game = {}

Scope.Game.GetEffect =
function (target, adderObject)
    return {}
end

Scope.Game.TriggerEvent = {}
Scope.Game.TriggerEvent.OnAdded =
function (target, adderObject)

end

Scope.Game.TriggerEvent.OnRemoved =
function (target, adderObject)

end

Scope.Game.TriggerEvent.OnTechFinished =
function (target, adderObject)

end

Scope.Game.TriggerEventPriority = {}
Scope.Game.TriggerEventPriority.OnTechFinished = 10