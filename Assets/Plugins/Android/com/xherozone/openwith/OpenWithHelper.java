package com.xherozone.openwith;

import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;

import androidx.core.content.FileProvider;

import java.io.File;

public class OpenWithHelper {

    // authority = ${applicationId}.fileprovider
    // bạn phải khai báo FileProvider trong AndroidManifest + file_paths.xml
    public static void openImage(Context context, String absolutePath, String extraText) {
        File file = new File(absolutePath);

        Uri uri;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            String authority = context.getPackageName() + ".fileprovider";
            uri = FileProvider.getUriForFile(context, authority, file);
        } else {
            uri = Uri.fromFile(file);
        }

        Intent viewIntent = new Intent(Intent.ACTION_VIEW);
        viewIntent.setDataAndType(uri, "image/png");
        viewIntent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);

        // Cho phép một số app nhận kèm text (không đảm bảo app nào cũng dùng)
        if (extraText != null && !extraText.isEmpty()) {
            viewIntent.putExtra(Intent.EXTRA_TEXT, extraText);
        }

        Intent chooser = Intent.createChooser(viewIntent, "Mở bằng");
        chooser.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        context.startActivity(chooser);
    }
}
