using UnityEngine;
using System.Collections;
using Unity.Entities;

public class DOTSTools
{

    public static void SetOrAdd<T>(EntityManager entityManager, Entity ent, T t) where T : struct, IComponentData
    {
        if (entityManager.HasComponent<T>(ent))
        {
            entityManager.SetComponentData<T>(ent, t);
        }
        else
        {
            entityManager.AddComponentData<T>(ent, t);
        }
    }
}
