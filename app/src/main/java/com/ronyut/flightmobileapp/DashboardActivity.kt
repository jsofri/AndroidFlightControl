package com.ronyut.flightmobileapp

import android.os.Bundle
import android.widget.SeekBar
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.ronyut.flightmobileapp.API.RequestHandler
import com.squareup.picasso.MemoryPolicy
import com.squareup.picasso.NetworkPolicy
import com.squareup.picasso.Picasso
import kotlinx.android.synthetic.main.activity_dashboard.*
import kotlinx.coroutines.*
import ninja.eigenein.joypad.JoypadView
import kotlin.math.abs


class DashboardActivity : AppCompatActivity(), JoypadView.Listener, CoroutineScope by MainScope() {
    private var baseUrl: String? = null
    private var jobScreenshot: Job? = Job()
    private var jobPost: Job? = Job()

    private var aileron: Double = 0.0
    private var rudder: Double = 0.0
    private var elevator: Double = 0.0
    private var throttle: Double = 0.5


    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_dashboard)
        supportActionBar?.hide()

        // get base api URL
        when (val url = intent.getStringExtra(MainActivity.EXTRA_MESSAGE)) {
            null -> toast("invalid Url")
            else -> baseUrl = url
        }

        setListeners()
    }

    // set all listeners
    private fun setListeners() {
        setRudderListener()
        joypad.setListener(this)
        getScreenshot()
    }

    // cancel job to stop getting screenshots when app is minimized
    override fun onStop() {
        super.onStop()
        println("BACKGROUND")
        jobScreenshot?.cancel()
    }

    // cancel job to stop getting screenshots when app is minimized
    override fun onDestroy() {
        super.onDestroy()
        cancel()
    }

    // resume job to resume getting screenshots when app is maximized
    override fun onStart() {
        super.onStart()
        jobScreenshot = Job()
        getScreenshot()
    }

    /*
    An async function for getting a screenshot
     */
    private fun getScreenshot() {
        // TODO: update the screenshot every second
        // run the co-routine to get a screenshot
        jobScreenshot = launch {
            while (true) {
                jobScreenshot?.ensureActive()
                println("Screenshot active? " + jobScreenshot?.isActive)
                println("screenshot!")
                Picasso.get()
                    .load(baseUrl + RequestHandler.URL_API_SCREENSHOT)
                    .networkPolicy(NetworkPolicy.NO_CACHE)
                    .memoryPolicy(MemoryPolicy.NO_CACHE)
                    .noPlaceholder()
                    .into(screenshot)
                delay(1000)

            }
        }
    }

    /*
    Post the flight data and makes a toast in case of error
     */
    private fun sendFlightData() {
        val flightData =
            FlightData(aileron, elevator, rudder, throttle)

        jobPost = launch {
            try {
                val requestHandler = RequestHandler(this@DashboardActivity, baseUrl)
                requestHandler.postFlightData(flightData) {}
            } catch (e: Exception) {
                when (e) {
                    is ServerUpException -> toast(codeToText(e.message?.toInt()))
                    else -> toast(e.message)
                }
            }
        }
    }

    /*
    Convert a status code into an explanatory string
     */
    private fun codeToText(code: Int?): String {
        return when (code) {
            200 -> "Success"
            else -> "Fail"
        }
    }

    /*
    Make a toast
     */
    private fun toast(text: String?) {
        Toast.makeText(applicationContext, text, Toast.LENGTH_SHORT).show()
    }

    /*
    When the joystick handle is released
     */
    override fun onUp() {}

    /*
    When the joystick handle is being moved
     */
    override fun onMove(dist: Float, x: Float, y: Float) {
        var doSend = false
        val xNew = x.toDouble()
        val yNew = y.toDouble()

        val changeX = abs(xNew - aileron) / 2
        val changeY = abs(yNew - elevator) / 2

        if (changeX >= 0.01) {
            println("Change X: $changeX")
            aileron = xNew
            doSend = true
        }

        if (changeY >= 0.01) {
            println("Change Y: $changeY")
            elevator = yNew
            doSend = true
        }

        if (doSend) {
            sendFlightData()
        }
    }

    /*
    Set the rudder seekbar listener
     */
    private fun setRudderListener() {
        rudder_seekbar.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                val newProgress = (progress.toDouble() - 50) / 50
                val change = abs((rudder * 50) + 50 - progress)
                if (change >= 1) {
                    rudder = newProgress
                    // run the co-routine to post flight data
                    sendFlightData()
                }
            }

            override fun onStartTrackingTouch(seekBar: SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: SeekBar?) {}
        })
    }
}