﻿Type = "Modifier"

Name = "Default"
TargetType = "Game"

AdditionalDesc = "DefaultDesc"

Scope = {}
Scope.Game = {}

Scope.Game.GetEffect =
function (target, adderGuid)
    return {}
end

Scope.Game.CheckCondition =
function (target, adderGuid)
    return false
end

Scope.Game.TriggerEvent = {}
Scope.Game.TriggerEvent.OnAdded =
function (target, adderGuid)

end

Scope.Game.TriggerEvent.OnRemoved =
function (target, adderGuid)

end

Scope.Game.TriggerEvent.OnTechFinished =
function (target, adderGuid)

end