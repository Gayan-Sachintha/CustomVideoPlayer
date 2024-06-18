using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Threading;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace CustomVideoPlayer
{
    public partial class MainWindow : Window
    {
        private bool isPaused = false;
        private DispatcherTimer recordingCheckTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeRecordingCheck();
        }

        private void InitializeRecordingCheck()
        {
            recordingCheckTimer = new DispatcherTimer();
            recordingCheckTimer.Interval = TimeSpan.FromSeconds(10); // Check every 10 seconds
            recordingCheckTimer.Tick += RecordingCheckTimer_Tick;
            recordingCheckTimer.Start();
        }

        private void RecordingCheckTimer_Tick(object sender, EventArgs e)
        {
            DetectAndStopRecording();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                mediaElement.Source = new Uri(openFileDialog.FileName);
                mediaElement.Play();
                isPaused = false;
                btnPause.Content = "Pause";
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                if (isPaused)
                {
                    mediaElement.Play();
                    btnPause.Content = "Pause";
                }
                else
                {
                    mediaElement.Pause();
                    btnPause.Content = "Play";
                }
                isPaused = !isPaused;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaElement.Source != null)
            {
                mediaElement.Stop();
                mediaElement.Source = null;
                isPaused = false;
                btnPause.Content = "Pause";
            }
        }

        private async void btnPlayOnline_Click(object sender, RoutedEventArgs e)
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
                        isPaused = false;
                        btnPause.Content = "Pause";
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
                        isPaused = false;
                        btnPause.Content = "Pause";
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

        private void DetectAndStopRecording()
        {
            string[] recordingProcesses = { "obs", "otherRecordingSoftware" }; 
            foreach (var processName in recordingProcesses)
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Any())
                {
                    foreach (var process in processes)
                    {
                        try
                        {
                            process.Kill();
                            MessageBox.Show($"Detected and stopped recording process: {processName}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error stopping process {processName}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
