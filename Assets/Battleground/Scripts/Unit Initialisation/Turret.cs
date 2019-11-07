using UnityEngine;

public class Turret : MonoBehaviour {
    public TurretInfo definition;
    public Team team;

    void Start() {
        TurretInstantiator.instantiate(team, definition, null);
    }
}
