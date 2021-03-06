﻿using UnityEngine;
using System.Collections;
using System;
using MumbleProto;

namespace Mumble {
    [RequireComponent(typeof(AudioSource))]
    public class MumbleAudioPlayer : MonoBehaviour {

        public float Gain = 1;
        public UInt32 Session { get; private set; }
        /// <summary>
        /// Notification that a new audio sample is available for processing
        /// It will be called on the audio thread
        /// It will contain the audio data, which you may want to process in
        /// your own code, and it contains the percent of the data left
        /// un-read
        /// </summary>
        public Action<float[], float> OnAudioSample;

        private MumbleClient _mumbleClient;
        private AudioSource _audioSource;
        private bool _isPlaying = false;

        void Start() {
            _audioSource = GetComponent<AudioSource>();
        }
        public string GetUsername()
        {
            if (_mumbleClient == null)
                return null;
            UserState state = _mumbleClient.GetUserFromSession(Session);
            if(state != null)
                return state.Name;
            return null;
        }
        public void Initialize(MumbleClient mumbleClient, UInt32 session)
        {
            //Debug.Log("Initialized " + session, this);
            Session = session;
            _mumbleClient = mumbleClient;
        }
        public void Reset()
        {
            _mumbleClient = null;
            Session = 0;
            OnAudioSample = null;
            _isPlaying = false;
            if(_audioSource != null)
                _audioSource.Stop();
        }
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_mumbleClient == null || !_mumbleClient.ConnectionSetupFinished)
                return;

            int numRead = _mumbleClient.LoadArrayWithVoiceData(Session, data, 0, data.Length);
            float percentUnderrun = 1f - numRead / data.Length;

            if (OnAudioSample != null)
                OnAudioSample(data, percentUnderrun);

            //Debug.Log("playing audio with avg: " + data.Average() + " and max " + data.Max());
            if (Gain == 1)
                return;

            for (int i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i] * Gain, -1f, 1f);
            //Debug.Log("playing audio with avg: " + data.Average() + " and max " + data.Max());
        }
        void Update()
        {
            if (_mumbleClient == null)
                return;
            if (!_isPlaying && _mumbleClient.HasPlayableAudio(Session))
            {
                _audioSource.Play();
                _isPlaying = true;
                Debug.Log("Playing audio");
            }
            else if(_isPlaying && !_mumbleClient.HasPlayableAudio(Session))
            {
                _audioSource.Stop();
                _isPlaying = false;
                Debug.Log("Stopping audio");
            }
        }
    }
}
