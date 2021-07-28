# Performance Settings

You can improve the performance of Files by modifying the following settings in the `Experimental` settings page.

**Cache files and folders**

After navigating to a directory, Files can cache the files/folders list and use it the next time you navigate to the same directory (this should result in the list of items loading faster).


**Preemptive cache parallel limit**

To further increase performance when navigating to a directory, you can set up preemptive caching. After navigating to any directory, the preemptive cache will go through all the folders in this directory and save them into the cache. This can make navigating into sub directories faster.

Preemptive caching is an expensive job for both the disk and also a little bit on the CPU. This is why you can set the parallel limit for this operations. If you set it to 0, then the preemptive cache will be turned off. For faster drives, such as NVMe drives, the limit can be set higher with less of an impact on your device.
