using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Observer : MonoBehaviour
{
    public Transform player;
    public GameEnding gameEnding;

    //Detection settings
    public float fieldOfViewThreshold = 0.5f;
    public float detectionSpeed = 0.5f;
    private float m_CurrentAwareness = 0f;

    // For effects
    private static int s_EnemiesDetectingPlayer = 0;
    private static ParticleSystem s_PlayerSweat;
    private static AudioSource s_PlayerHeartbeat;
    private bool m_IsPlayerInRange;
    private bool m_ThisEnemySeesPlayer;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            m_IsPlayerInRange = true;

            // Sound and particles
            if (s_PlayerSweat == null) s_PlayerSweat = player.Find("Sweat")?.GetComponent<ParticleSystem>();
            if (s_PlayerHeartbeat == null) s_PlayerHeartbeat = player.Find("Heartbeat")?.GetComponent<AudioSource>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            m_IsPlayerInRange = false;

            // Track enemies that see player
            if (m_ThisEnemySeesPlayer)
            {
                m_ThisEnemySeesPlayer = false;
                s_EnemiesDetectingPlayer--;
            }

            m_CurrentAwareness = 0f;
            UpdateGlobalEffects();
        }
    }

    void Update()
    {
        bool canSeeJohn = false;

        if (m_IsPlayerInRange)
        {
            Vector3 direction = (player.position + Vector3.up) - transform.position;

            // DOT PRODUCT: Vision check
            float dotProduct = Vector3.Dot(transform.forward, direction.normalized);

            if (dotProduct > fieldOfViewThreshold)
            {
                Ray ray = new Ray(transform.position, direction);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.transform == player)
                {
                    canSeeJohn = true;
                }
            }
        }

        // LINEAR INTERPOLATION (Lerp): Awareness calculation
        float target = canSeeJohn ? 1f : 0f;
        m_CurrentAwareness = Mathf.MoveTowards(m_CurrentAwareness, target, detectionSpeed * Time.deltaTime);

        // Track if THIS specific enemy currently sees John
        if (m_CurrentAwareness > 0f && !m_ThisEnemySeesPlayer)
        {
            m_ThisEnemySeesPlayer = true;
            s_EnemiesDetectingPlayer++;
        }
        else if (m_CurrentAwareness <= 0f && m_ThisEnemySeesPlayer)
        {
            m_ThisEnemySeesPlayer = false;
            s_EnemiesDetectingPlayer--;
        }

        UpdateGlobalEffects();

        if (m_CurrentAwareness >= 1f)
        {
            gameEnding.CaughtPlayer();
        }
    }

    void UpdateGlobalEffects()
    {
        // Only plays if at least one enemy (count > 0) sees the player
        if (s_EnemiesDetectingPlayer > 0)
        {
            if (s_PlayerSweat != null && !s_PlayerSweat.isPlaying) s_PlayerSweat.Play();
            if (s_PlayerHeartbeat != null)
            {
                if (!s_PlayerHeartbeat.isPlaying) s_PlayerHeartbeat.Play();

                // Use the highest awareness level for volume
                s_PlayerHeartbeat.volume = Mathf.Max(s_PlayerHeartbeat.volume, 0.5f);
            }
        }
        else
        {
            // Only stop if NO enemies see the player anymore
            if (s_PlayerSweat != null && s_PlayerSweat.isPlaying) s_PlayerSweat.Stop();
            if (s_PlayerHeartbeat != null && s_PlayerHeartbeat.isPlaying) s_PlayerHeartbeat.Stop();
            if (s_PlayerHeartbeat != null) s_PlayerHeartbeat.volume = 0f;
        }
    }
}
