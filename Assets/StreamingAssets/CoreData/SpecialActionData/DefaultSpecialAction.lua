Type = "SpecialAction"

Name = "Default"
TargetType = "Game"
NeedCoordinate = false

function IsAvailable (owner)
    return false
end

function GetAvailableTiles (owner)
    local result = {}
    return result
end

function PreviewEffectRange (owner, coord)
    local result = {}
    return result
end

function GetCost (owner, coord)
    local result = {}
    return result
end

function DoAction (owner, coord)
    return false
end