Sufni.Bridge
============

Sufni.Bridge\* is a work-in-progress cross-platform (.NET core / Avalonia) application to process
recorded sessions directly from a [Sufni Suspension Telemetry](https://github.com/sghctoma/sst)
DAQ either via the DAQ's server, or its mass storage device (MSC) mode. As of
now, the application has a limited functionality compared to the
[web-based dashboard](https://github.com/sghctoma/sst/wiki/03-Dashboard), but
it does not require an internet connection.

<style>
  /* Container for the gallery */
  .gallery {
    display: flex;
    overflow-x: auto;
    white-space: nowrap;
  }

  /* Styling for each image */
  .gallery img {
    flex: 0 0 auto;
    width: auto;
    height: 400px;
    margin-right: 10px;
  }

  /* Optional: Remove scrollbar styles for a cleaner look (not supported on all browsers) */
  .gallery::-webkit-scrollbar {
    display: none;
  }
  .gallery {
    -ms-overflow-style: none;
    scrollbar-width: none;
  }
</style>

<div class="gallery">
  <img src="pics/import.png" alt="Import">
  <img src="pics/sessions.png" alt="Sessions">
  <img src="pics/spring.png" alt="Spring">
  <img src="pics/damper.png" alt="Damper">
  <img src="pics/balance.png" alt="Balance">
  <img src="pics/notes.png" alt="Notes">
  <img src="pics/linkage.png" alt="Linkage">
</div>
<br />

Important limitations:

 - No travel and velocity graphs, only histograms and balance
 - No interactive graphs, GPS map, video

\* *Pronounced SHOOF-nee dot bridge. Sufni means tool shed in Hungarian, but
also used as an adjective denoting something as DIY, garage hack, etc.*
