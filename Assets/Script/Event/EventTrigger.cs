using UnityEngine;

public class EventTrigger : MonoBehaviour
{
    public FixedEventManager fixedEventManager;
    bool isTriggered;
    PlayerMove mover;
    Collider2D trigger;
    void Start()
    {
        mover = FindObjectOfType<PlayerMove>();
        trigger = GetComponent<Collider2D>();
    }
    void Update()
    {
        // A song-time crossing cannot be missed when an automatic jump passes over the small event box.
        if (isTriggered || mover == null || !mover.HasRhythmPath || mover.isPaused || trigger == null) return;
        var clock = FindObjectOfType<RhythmClock>();
        var body = mover.GetComponent<Collider2D>();
        if (clock != null && clock.TravelEnabled && !clock.IsPaused && clock.HasStarted &&
            mover.transform.position.x + (body != null ? body.bounds.extents.x : 0) >= trigger.bounds.min.x - 0.01f)
            Begin();
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isTriggered && collision.CompareTag("Player")) Begin();
    }
    void Begin()
    {
        isTriggered = true;
        if (fixedEventManager != null) fixedEventManager.StartFixedEvent();
    }
}