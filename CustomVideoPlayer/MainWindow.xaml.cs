using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace CustomVideoPlayer
{
    public partial class MainWindow : Window
    {
        private bool _isPaused = false;
        private DispatcherTimer _recordingCheckTimer;
        private DispatcherTimer _timelineUpdateTimer;
        private bool _isRecordingDetected = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeRecordingCheck();
            InitializeTimelineUpdate();
        }

        private void InitializeRecordingCheck()
        {
            _recordingCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _recordingCheckTimer.Tick += RecordingCheckTimerTick;
            _recordingCheckTimer.Start();
        }

        private void InitializeTimelineUpdate()
        {
            _timelineUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timelineUpdateTimer.Tick += TimelineUpdateTimerTick;
            _timelineUpdateTimer.Start();
        }

        private void RecordingCheckTimerTick(object sender, EventArgs e)
        {
            DetectRecording();
        }

        private void TimelineUpdateTimerTick(object sender, EventArgs e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                timelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                timelineSlider.Value = mediaElement.Position.TotalSeconds;
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                mediaElement.Source = new Uri(openFileDialog.FileName);
                mediaElement.Play();
                _isPaused = false;
                btnPause.Content = "Pause";
                _isRecordingDetected = false;
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (_isPaused)
                {
                    mediaElement.Play();
                    btnPause.Content = "Pause";
                }
                else
                {
                    mediaElement.Pause();
                    btnPause.Content = "Play";
                }
                _isPaused = !_isPaused;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                mediaElement.Stop();
                mediaElement.Source = null;
                _isPaused = false;
                btnPause.Content = "Pause";
                _isRecordingDetected = false;
            }
        }

        private async void BtnPlayOnline_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = urlTextBox.Text;
            if (!string.IsNullOrEmpty(videoUrl))
            {
                if (videoUrl.Contains("youtube.com") || videoUrl.Contains("youtu.be"))
                {
                    try
                    {
                        var youtube = new YoutubeClient();
                        var video = await youtube.Videos.GetAsync(videoUrl);

                        if (video == null)
                        {
                            MessageBox.Show("Error: Video could not be retrieved.");
                            return;
                        }

                        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                        if (streamManifest == null)
                        {
                            MessageBox.Show("Error: Stream manifest could not be retrieved.");
                            return;
                        }

                        var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                        if (streamInfo == null)
                        {
                            MessageBox.Show("Error: No suitable stream found.");
                            return;
                        }

                        mediaElement.Source = new Uri(streamInfo.Url);
                        mediaElement.Play();
                        _isPaused = false;
                        btnPause.Content = "Pause";
                        _isRecordingDetected = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error playing YouTube video: {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        mediaElement.Source = new Uri(videoUrl);
                        mediaElement.Play();
                        _isPaused = false;
                        btnPause.Content = "Pause";
                        _isRecordingDetected = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error playing video: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid URL.");
            }
        }

        private void DetectRecording()
        {
            string[] recordingProcesses = { "obs", "camtasia", "bandicam", "fraps", "xsplit", "snagit" };
            foreach (var processName in recordingProcesses)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Any())
                {
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (!_isRecordingDetected)
                            {
                                MessageBox.Show($"Detected recording process: {processName}. Video will be blacked out.");
                                _isRecordingDetected = true;
                            }
                            mediaElement.Visibility = Visibility.Hidden;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error stopping process {processName}: {ex.Message}");
                        }
                    }
                }
                else if (_isRecordingDetected)
                {
                    mediaElement.Visibility = Visibility.Visible;
                    _isRecordingDetected = false;
                }
            }
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Position = TimeSpan.FromSeconds(timelineSlider.Value);
            }
        }

        #region Windows API Declarations

        const int WDA_NONE = 0;
        const int WDA_MONITOR = 1;

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        #endregion
    }
}
