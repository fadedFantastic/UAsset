import android.os.Build;
import android.os.Environment;
import android.os.StatFs;
import java.math.BigInteger;

public class DiskUtils {

    /**
     * Calculates available space on disk.
     * @param external  Queries external disk if true, queries internal disk otherwise.
     * @return Available disk space in Byte.
     */
    public static long availableSpace(boolean external)
    {
        long availableBlocks;
        long blockSize;

        StatFs statFs = getStats(external);
        if (Build.VERSION.SDK_INT < 18){
            availableBlocks = statFs.getAvailableBlocks();
            blockSize = statFs.getBlockSize();
        }
        else
        {
            availableBlocks = statFs.getAvailableBlocksLong();
            blockSize = statFs.getBlockSizeLong();
        }

        BigInteger free = BigInteger.valueOf(availableBlocks).multiply(BigInteger.valueOf(blockSize));
        return free.longValue();
    }

    private static StatFs getStats(boolean external){
        String path;

        if (external){
            path = Environment.getExternalStorageDirectory().getAbsolutePath();
        }
        else{
            path = Environment.getDataDirectory().getAbsolutePath();
        }

        return new StatFs(path);
    }
}