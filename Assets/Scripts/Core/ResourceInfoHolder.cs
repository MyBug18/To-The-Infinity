﻿namespace Core
{
    public enum ResourceType
    {
        PlanetaryResource,
        GlobalResource,
        Research,
        Factor,
    }

    public class ResourceInfoHolder
    {
        public string Name { get; }

        public ResourceType ResourceType { get; }

        public ResourceInfoHolder(string name, ResourceType resourceType)
        {
            Name = name;
            ResourceType = resourceType;
        }
    }
}