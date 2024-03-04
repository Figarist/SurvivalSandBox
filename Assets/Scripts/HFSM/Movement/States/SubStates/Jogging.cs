using UnityEngine;

namespace HFSM.Movement.States.SubStates {
    public class Jogging: Grounded {
        public Jogging(StateMachine stateMachine) : base("Jogging", stateMachine) {
        }

        public override void Update() {
            base.Update();
            CheckForTransition();
            
        }

        private void CheckForTransition() {
            if (new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).magnitude <= 0) {
                sm.ChangeState(sm.idleState);
            }
            if (Input.GetKeyDown(KeyCode.LeftShift)) {
                sm.ChangeState(sm.runningState);
            }
        }
    }
}