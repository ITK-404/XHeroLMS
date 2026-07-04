package com.xherozone.otp;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

public final class SmsConsentActivity extends Activity {
    private static final String TAG = "XHeroSmsConsent";
    private static final String EXTRA_CONSENT_INTENT = "com.xherozone.otp.CONSENT_INTENT";
    private static final int REQUEST_SMS_CONSENT = 7341;

    private boolean launched;

    public static void start(Activity source, Intent consentIntent) {
        Log.d(TAG, "Starting SMS consent wrapper activity");
        Intent intent = new Intent(source, SmsConsentActivity.class);
        intent.putExtra(EXTRA_CONSENT_INTENT, consentIntent);
        source.startActivity(intent);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        launched = savedInstanceState != null && savedInstanceState.getBoolean("launched", false);

        if (!launched) {
            launchConsentDialog();
        }
    }

    @Override
    protected void onSaveInstanceState(Bundle outState) {
        outState.putBoolean("launched", launched);
        super.onSaveInstanceState(outState);
    }

    private void launchConsentDialog() {
        try {
            Intent consentIntent = getIntent().getParcelableExtra(EXTRA_CONSENT_INTENT);
            if (consentIntent == null) {
                Log.w(TAG, "Consent intent is null");
                SmsOtpRetriever.handleConsentResult(Activity.RESULT_CANCELED, null);
                finish();
                return;
            }

            launched = true;
            Log.d(TAG, "Launching SMS consent dialog");
            startActivityForResult(consentIntent, REQUEST_SMS_CONSENT);
        } catch (Throwable t) {
            Log.w(TAG, "Unable to launch SMS consent dialog", t);
            SmsOtpRetriever.handleConsentResult(Activity.RESULT_CANCELED, null);
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQUEST_SMS_CONSENT) {
            Log.d(TAG, "SMS consent dialog resultCode=" + resultCode + ", hasData=" + (data != null));
            SmsOtpRetriever.handleConsentResult(resultCode, data);
            finish();
        }
    }
}
