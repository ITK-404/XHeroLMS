package com.xherozone.otp;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.pm.Signature;
import android.os.Build;
import android.os.Bundle;
import android.util.Base64;
import android.util.Log;

import com.google.android.gms.auth.api.phone.SmsRetriever;
import com.google.android.gms.auth.api.phone.SmsRetrieverClient;
import com.google.android.gms.common.api.CommonStatusCodes;
import com.google.android.gms.common.api.Status;
import com.unity3d.player.UnityPlayer;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public final class SmsOtpRetriever {
    private static final String TAG = "XHeroSmsOtpRetriever";
    private static final int HASH_BYTE_COUNT = 9;
    private static final int HASH_BASE64_CHAR_COUNT = 11;

    private static BroadcastReceiver receiver;
    private static boolean registered;
    private static String gameObjectName;
    private static String successMethodName;
    private static String errorMethodName;
    private static String otpRegex = "(?<!\\d)\\d{6}(?!\\d)";

    private SmsOtpRetriever() {
    }

    public static void startListening(
            String unityGameObjectName,
            String unitySuccessMethodName,
            String unityErrorMethodName,
            String regex,
            boolean useUserConsentFallback,
            String senderPhoneNumber
    ) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            sendUnityError(unityGameObjectName, unityErrorMethodName, "activity_null");
            return;
        }

        gameObjectName = unityGameObjectName;
        successMethodName = unitySuccessMethodName;
        errorMethodName = unityErrorMethodName;
        if (regex != null && regex.length() > 0) {
            otpRegex = regex;
        }

        Log.d(TAG, "startListening target=" + gameObjectName
                + ", success=" + successMethodName
                + ", error=" + errorMethodName
                + ", consent=" + useUserConsentFallback);

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    stopListeningInternal(activity);
                    registerReceiver(activity);

                    SmsRetrieverClient client = SmsRetriever.getClient(activity);
                    client.startSmsRetriever()
                            .addOnSuccessListener(unused ->
                                    Log.d(TAG, "SMS Retriever started"))
                            .addOnFailureListener(e -> {
                                Log.w(TAG, "SMS Retriever start failed", e);
                                if (!useUserConsentFallback) {
                                    stopListeningInternal(activity);
                                }
                                sendUnityError("start_failed:" + e.getMessage());
                            });

                    if (useUserConsentFallback) {
                        client.startSmsUserConsent(senderPhoneNumber)
                                .addOnSuccessListener(unused ->
                                        Log.d(TAG, "SMS User Consent started"))
                                .addOnFailureListener(e -> {
                                    Log.w(TAG, "SMS User Consent start failed", e);
                                    sendUnityError("consent_start_failed:" + e.getMessage());
                                });
                    }
                } catch (Throwable t) {
                    Log.w(TAG, "SMS Retriever setup failed", t);
                    stopListeningInternal(activity);
                    sendUnityError("setup_failed:" + t.getMessage());
                }
            }
        });
    }

    public static void stopListening() {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            registered = false;
            receiver = null;
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                stopListeningInternal(activity);
            }
        });
    }

    public static String getAppSignatureHash() {
        Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            return "";
        }

        String[] hashes = getAppSignatureHashes(activity.getApplicationContext());
        return hashes.length > 0 ? hashes[0] : "";
    }

    private static void registerReceiver(final Activity activity) {
        receiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                if (intent == null || !SmsRetriever.SMS_RETRIEVED_ACTION.equals(intent.getAction())) {
                    return;
                }

                Bundle extras = intent.getExtras();
                if (extras == null) {
                    stopListeningInternal(activity);
                    sendUnityError("extras_null");
                    return;
                }

                Status status = (Status) extras.get(SmsRetriever.EXTRA_STATUS);
                if (status == null) {
                    stopListeningInternal(activity);
                    sendUnityError("status_null");
                    return;
                }

                if (status.getStatusCode() == CommonStatusCodes.SUCCESS) {
                    String message = (String) extras.get(SmsRetriever.EXTRA_SMS_MESSAGE);
                    if (message != null && message.length() > 0) {
                        String otp = extractOtp(message);
                        stopListeningInternal(activity);

                        if (otp.length() > 0) {
                            sendUnitySuccess(otp);
                        } else {
                            sendUnityError("otp_not_found");
                        }
                        return;
                    }

                    Object consentObject = extras.get(SmsRetriever.EXTRA_CONSENT_INTENT);
                    if (consentObject instanceof Intent) {
                        Intent consentIntent = (Intent) consentObject;
                        Log.d(TAG, "SMS User Consent intent received; opening consent dialog");
                        stopListeningInternal(activity);
                        SmsConsentActivity.start(activity, consentIntent);
                        return;
                    }

                    stopListeningInternal(activity);
                    sendUnityError("sms_payload_empty");
                    return;
                }

                if (status.getStatusCode() == CommonStatusCodes.TIMEOUT) {
                    stopListeningInternal(activity);
                    sendUnityError("timeout");
                    return;
                }

                stopListeningInternal(activity);
                sendUnityError("status_code:" + status.getStatusCode());
            }
        };

        IntentFilter filter = new IntentFilter(SmsRetriever.SMS_RETRIEVED_ACTION);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            activity.registerReceiver(
                    receiver,
                    filter,
                    SmsRetriever.SEND_PERMISSION,
                    null,
                    Context.RECEIVER_EXPORTED
            );
        } else {
            activity.registerReceiver(receiver, filter, SmsRetriever.SEND_PERMISSION, null);
        }
        registered = true;
    }

    private static void stopListeningInternal(Context context) {
        if (!registered || receiver == null || context == null) {
            registered = false;
            receiver = null;
            return;
        }

        try {
            context.unregisterReceiver(receiver);
        } catch (Throwable ignored) {
        }

        registered = false;
        receiver = null;
    }

    private static String extractOtp(String message) {
        if (message == null) {
            return "";
        }

        try {
            Matcher matcher = Pattern.compile(otpRegex).matcher(message);
            if (matcher.find()) {
                return matcher.group(0);
            }
        } catch (Throwable t) {
            Log.w(TAG, "Invalid OTP regex: " + otpRegex, t);
        }

        Matcher fallback = Pattern.compile("\\d{6}").matcher(message);
        return fallback.find() ? fallback.group(0) : "";
    }

    public static void handleConsentResult(int resultCode, Intent data) {
        Log.d(TAG, "handleConsentResult resultCode=" + resultCode + ", hasData=" + (data != null));

        if (resultCode != Activity.RESULT_OK || data == null) {
            sendUnityError("consent_cancelled");
            return;
        }

        String message = data.getStringExtra(SmsRetriever.EXTRA_SMS_MESSAGE);
        Log.d(TAG, "Consent returned SMS message length=" + (message == null ? 0 : message.length()));

        String otp = extractOtp(message);
        if (otp.length() > 0) {
            Log.d(TAG, "Consent OTP extracted length=" + otp.length());
            sendUnitySuccess(otp);
        } else {
            Log.w(TAG, "Consent message did not match OTP regex");
            sendUnityError("otp_not_found");
        }
    }

    private static void sendUnitySuccess(String payload) {
        if (gameObjectName == null || successMethodName == null) {
            Log.w(TAG, "Cannot send Unity success because target is missing");
            return;
        }
        Log.d(TAG, "Sending Unity success to " + gameObjectName + "." + successMethodName
                + " payloadLength=" + (payload == null ? 0 : payload.length()));
        UnityPlayer.UnitySendMessage(gameObjectName, successMethodName, payload == null ? "" : payload);
    }

    private static void sendUnityError(String payload) {
        Log.d(TAG, "Sending Unity error payload=" + payload);
        sendUnityError(gameObjectName, errorMethodName, payload);
    }

    private static void sendUnityError(String targetGameObject, String targetMethod, String payload) {
        if (targetGameObject == null || targetMethod == null) {
            Log.w(TAG, "Cannot send Unity error because target is missing");
            return;
        }
        Log.d(TAG, "Sending Unity error to " + targetGameObject + "." + targetMethod);
        UnityPlayer.UnitySendMessage(targetGameObject, targetMethod, payload == null ? "" : payload);
    }

    private static String[] getAppSignatureHashes(Context context) {
        try {
            String packageName = context.getPackageName();
            Signature[] signatures = getSignatures(context, packageName);
            String[] hashes = new String[signatures.length];

            for (int i = 0; i < signatures.length; i++) {
                hashes[i] = hash(packageName, signatures[i].toCharsString());
            }

            return hashes;
        } catch (Throwable t) {
            Log.w(TAG, "Unable to calculate SMS Retriever app hash", t);
            return new String[0];
        }
    }

    private static Signature[] getSignatures(Context context, String packageName) throws PackageManager.NameNotFoundException {
        PackageManager packageManager = context.getPackageManager();

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            PackageInfo packageInfo = packageManager.getPackageInfo(packageName, PackageManager.GET_SIGNING_CERTIFICATES);
            if (packageInfo.signingInfo == null) {
                return new Signature[0];
            }

            if (packageInfo.signingInfo.hasMultipleSigners()) {
                return packageInfo.signingInfo.getApkContentsSigners();
            }

            return packageInfo.signingInfo.getSigningCertificateHistory();
        }

        PackageInfo packageInfo = packageManager.getPackageInfo(packageName, PackageManager.GET_SIGNATURES);
        return packageInfo.signatures == null ? new Signature[0] : packageInfo.signatures;
    }

    private static String hash(String packageName, String signature) throws Exception {
        String appInfo = packageName + " " + signature;
        MessageDigest messageDigest = MessageDigest.getInstance("SHA-256");
        messageDigest.update(appInfo.getBytes(StandardCharsets.UTF_8));
        byte[] hashSignature = messageDigest.digest();
        hashSignature = Arrays.copyOfRange(hashSignature, 0, HASH_BYTE_COUNT);

        String base64Hash = Base64.encodeToString(
                hashSignature,
                Base64.NO_PADDING | Base64.NO_WRAP
        );
        return base64Hash.substring(0, HASH_BASE64_CHAR_COUNT);
    }
}
