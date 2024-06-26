using System.Collections;
using System.Linq;
using UnityEngine;

public class CharacterMovement : MonoBehaviour {
    private static readonly int WITHDRAW_SWORD_TRIGGER = Animator.StringToHash("WithdrawSword");
    private static readonly int SHEAT_SWORD_TRIGGER = Animator.StringToHash("SheatSword");
    private static readonly int IS_HITTING_BOOL = Animator.StringToHash("IsHitting");
    private static readonly int IS_CROUCHING_BOOL = Animator.StringToHash("IsCrouching");
    private static readonly int MOVING_STATE_INT = Animator.StringToHash("MovingState");
    private static readonly int JUMP_BOOL = Animator.StringToHash("Jump");
    private static readonly int IS_FALLING_BOOL = Animator.StringToHash("IsFalling");
    
    [SerializeField] private float speed; //5
    [SerializeField] private float rotationSpeed; // 0.2
    [SerializeField] private float gravitySpeed; // 35
    [SerializeField] private float jumpForce; // 11
    [SerializeField] private float sprintCoefficient; // 2

    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private Transform roofPoint;
    [SerializeField] private Animator playerAnimator;

    [SerializeField] private CapsuleCollider normalPlayerCollider;
    [SerializeField] private CapsuleCollider smallPlayerCollider;

    [SerializeField] private GameObject swordInBelt;
    [SerializeField] private GameObject swordInHand;
    [SerializeField] private GameObject damageNumber;

    private float gravity = -10;
    private float remainingSpeed;

    private bool isReadyToJump = true;
    private bool isReadyToLand;

    private int movingState; // 0 - Idle, 1 - Jogging, 2 - Sprint, 3 - Crouching
    private bool isCrouching;
    private bool isSprinting;

    private bool isPlayerOnGround;

    private bool canHit;
    private bool isHoldingSword;
    private bool isHitting;
    private float swordSlashAnimDuration;

    private void Awake() {
        characterController = GetComponent<CharacterController>();
        remainingSpeed = speed;
    }

    private CharacterController characterController;
    private void Update() {
        HandleSwordWithdraw();
        HandleHit();
        HandleJump();
        HandleGravity();
        Move();
    }

    private Vector3 direction;

    private void Move() {
        direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isSprinting) {
            if (!isCrouching || !Physics.Raycast(roofPoint.position, Vector3.up, 0.7f)) {
                isCrouching = !isCrouching;
                normalPlayerCollider.enabled = !isCrouching;
                smallPlayerCollider.enabled = isCrouching;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isCrouching) {
            isSprinting = !isSprinting;
        }

        movingState = 0;
        
        bool isPlayerMoving = direction.magnitude > 0;
        if (isPlayerMoving) {
            float rotationY = mainCamera.rotation.eulerAngles.y + Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, rotationY, 0),
                isPlayerOnGround ? rotationSpeed : rotationSpeed / 2);
            movingState = isSprinting ? 2 : 1;
        } else {
            isSprinting = false;
        }

        if (isPlayerMoving && isCrouching) {
            movingState = 3;
        }

        if (isHitting) {
            float rotationY = mainCamera.rotation.eulerAngles.y + Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, rotationY, 0),
                isPlayerOnGround ? rotationSpeed : rotationSpeed / 2);
        }

        playerAnimator.SetBool(IS_CROUCHING_BOOL, isCrouching);
        playerAnimator.SetInteger(MOVING_STATE_INT, movingState);
        
        direction = mainCamera.transform.TransformDirection(direction).normalized;
        direction.y = gravity;
        characterController.Move(direction * Time.deltaTime * (isSprinting ? speed * sprintCoefficient : speed));
    }

    private void HandleSwordWithdraw() {
        if (Input.GetKeyDown(KeyCode.Alpha1) && movingState == 0 && !isCrouching) {
            isHoldingSword = !isHoldingSword;
            remainingSpeed = speed;
            speed /= 5;
            StartCoroutine(ProceedSwordWithdraw());
        }
    }

    private void HandleHit() {
        if (Input.GetKey(KeyCode.Mouse0) && canHit && isHoldingSword && movingState == 0 && !isCrouching) {
            canHit = false;
            remainingSpeed = speed;
            speed /= 5;
            StartCoroutine(ProceedHit());
        }
    }

    private void HandleJump() {
        if (Input.GetKeyDown(KeyCode.Space) && isReadyToJump && !isCrouching) {
            isReadyToJump = false;
            playerAnimator.SetBool(JUMP_BOOL, true);
            if (movingState is 1 or 2) {
                gravity = jumpForce;
            } else {
                StartCoroutine(DelayJump());
            }

            StartCoroutine(CheckJump());
        }
    }

    private void HandleGravity() {
        isPlayerOnGround = Physics.OverlapSphere(groundPoint.position, 0.15f)
            .Where(x => !x.gameObject.CompareTag("Player"))
            .ToList().Count > 0;
        playerAnimator.SetBool(IS_FALLING_BOOL, !isPlayerOnGround);

        if (!isPlayerOnGround) {
            isReadyToLand = true;
            isReadyToJump = false;
        }

        if (isReadyToLand && isPlayerOnGround) {
            isReadyToLand = false;
            if (movingState is 1 or 2) {
                isReadyToJump = true;
                playerAnimator.SetBool(JUMP_BOOL, false);
            } else {
                StartCoroutine(DelayReadyToJump());
            }
        }

        gravity -= Time.deltaTime * gravitySpeed;
        gravity = Mathf.Clamp(gravity, isPlayerOnGround ? -3 : -30, 100);
    }

    private IEnumerator ProceedHit() {
        isHitting = true;
        playerAnimator.SetBool(IS_HITTING_BOOL, true);
        yield return new WaitForSeconds(0.1f);

        Collider enemy = Physics.OverlapSphere(hitPoint.position, 0.5f)
            .FirstOrDefault(c => c.gameObject.CompareTag("Enemy"));
        if (enemy is not null) {
            SkeletonManager skeletonManager = enemy.gameObject.GetComponent<SkeletonManager>();
            int damage = Random.Range(10, 20);
            // skeletonManager.SetDamage(damage);
            Vector3 point = enemy.gameObject.transform.position;
            
        }

        playerAnimator.SetBool(IS_HITTING_BOOL, false);
        yield return new WaitForSeconds(0.8f);
        speed = remainingSpeed;
        isHitting = false;
        yield return new WaitForSeconds(0.1f);
        canHit = true;
    }

    private IEnumerator ProceedSwordWithdraw() {
        playerAnimator.SetTrigger(isHoldingSword ? WITHDRAW_SWORD_TRIGGER : SHEAT_SWORD_TRIGGER);

        yield return new WaitForSeconds(isHoldingSword ? 0.55f : 1.3f);
        canHit = isHoldingSword;

        swordInHand.SetActive(isHoldingSword);
        swordInBelt.SetActive(!isHoldingSword);

        yield return new WaitForSeconds(0.2f);

        speed = remainingSpeed;
    }

    private IEnumerator DelayJump() {
        yield return new WaitForSeconds(0.3f);
        gravity = jumpForce;
    }

    private IEnumerator DelayReadyToJump() {
        yield return new WaitForSeconds(0.25f);
        isReadyToJump = true;
        playerAnimator.SetBool(JUMP_BOOL, false);
    }

    private IEnumerator CheckJump() {
        float delay = movingState is 1 or 2 ? 0.05f : 0.35f;
        yield return new WaitForSeconds(delay);
        if (isPlayerOnGround) {
            isReadyToJump = true;
            playerAnimator.SetBool(JUMP_BOOL, false);
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(roofPoint.position,
            new Vector3(roofPoint.position.x, roofPoint.position.y + 0.7f, roofPoint.position.z));
        Gizmos.DrawSphere(groundPoint.position, 0.15f);
        Gizmos.DrawSphere(hitPoint.position, 0.5f);
    }
}