Type = "TileMap"

Name = "Default"

function MakeTile (coord, noise, sharedStorage)
    local result = {}
    result.Name = "Default"
    result.ResDecider = sharedStorage.Get("random", nil).NextDouble()
    return result
end