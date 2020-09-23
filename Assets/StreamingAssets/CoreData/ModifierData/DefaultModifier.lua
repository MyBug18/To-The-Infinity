Type = "Modifier"

Name = "Default"
TargetType = "Game"

AdditionalDesc = "DefaultDesc"

Scope = {}
Scope.Game = {}

Scope.Game.GetEffect =
function (target)
    return {}
end

Scope.Game.CheckCondition =
function (target)
    return false
end

Scope.Game.OnAdded =
function (target)

end

Scope.Game.OnRemoved =
function (target)

end