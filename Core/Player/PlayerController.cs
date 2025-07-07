using System;
using System.Collections.Generic;

public partial class PlayerController
{
    public uint AccountId { get; set; }
    public uint EntityId { get; set; }

    public Entity? Entity
    {
        get
        {
            if(EntityManager.TryGet(EntityId, out var entity))            
                return entity;
            
            return null;
        }
    }
}
