using UnityEngine;

namespace StatePattern {
    public class Idle : State {
    
        public override void Enter() {
            base.Enter();
            Debug.Log("idle entered");
        }
        
        public override void Update(PlayerSM context) {
        
            //if Space.pressed -> ToggleState(Jump)
            //if W or A or S or D -> ToggleState(Walk)

        }

        public override void Exit() {
            base.Exit();
            Debug.Log(("idle exit"));
        }
    }
}

