namespace ScadaNet.AspNetCore;

public static class ScadaNetOptionsValidator
{
    public static void Validate(ScadaNetOptions options)
    {
        var errors = GetErrors(options);

        if (errors.Count > 0)
        {
            throw new ScadaNetOptionsValidationException(errors);
        }
    }

    public static IReadOnlyList<string> GetErrors(ScadaNetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();
        var deviceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var devicesByName = new Dictionary<string, Runtime.DeviceDefinition>(StringComparer.OrdinalIgnoreCase);
        var pollingGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in options.Devices)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
            {
                errors.Add("Device name cannot be empty.");
            }
            else if (!deviceNames.Add(device.Name))
            {
                errors.Add($"Device '{device.Name}' is registered more than once.");
            }
            else
            {
                devicesByName[device.Name] = device;
            }

            if (string.IsNullOrWhiteSpace(device.Driver))
            {
                errors.Add($"Device '{device.Name}' driver cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(device.Address))
            {
                errors.Add($"Device '{device.Name}' address cannot be empty.");
            }

            if (device.Timeout <= TimeSpan.Zero)
            {
                errors.Add($"Device '{device.Name}' timeout must be greater than zero.");
            }

            if (device.WritableAddresses.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"Device '{device.Name}' contains an empty writable address.");
            }

            var signalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var signalAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var signal in device.Signals)
            {
                if (string.IsNullOrWhiteSpace(signal.Name))
                {
                    errors.Add($"Device '{device.Name}' contains a signal with an empty name.");
                }
                else if (!signalNames.Add(signal.Name))
                {
                    errors.Add($"Device '{device.Name}' contains signal '{signal.Name}' more than once.");
                }

                if (string.IsNullOrWhiteSpace(signal.Address))
                {
                    errors.Add($"Device '{device.Name}' signal '{signal.Name}' address cannot be empty.");
                }
                else if (!signalAddresses.Add(signal.Address))
                {
                    errors.Add($"Device '{device.Name}' contains signal address '{signal.Address}' more than once.");
                }

                if (signal.ElementCount <= 0)
                {
                    errors.Add($"Device '{device.Name}' signal '{signal.Name}' element count must be greater than zero.");
                }

                if (signal.MinValue.HasValue &&
                    signal.MaxValue.HasValue &&
                    signal.MinValue.Value > signal.MaxValue.Value)
                {
                    errors.Add($"Device '{device.Name}' signal '{signal.Name}' minimum value cannot be greater than maximum value.");
                }

                if (signal.Writable)
                {
                    if (!device.WritesEnabled)
                    {
                        errors.Add(
                            $"Device '{device.Name}' signal '{signal.Name}' is writable, but device writes are disabled.");
                    }
                    else if (!string.IsNullOrWhiteSpace(signal.Address) && !device.CanWrite(signal.Address))
                    {
                        errors.Add(
                            $"Device '{device.Name}' signal '{signal.Name}' address '{signal.Address}' is not allowed by the device write policy.");
                    }
                }
            }
        }

        foreach (var group in options.PollingGroups)
        {
            var groupName = string.IsNullOrWhiteSpace(group.Name)
                ? group.DeviceName
                : group.Name;

            if (string.IsNullOrWhiteSpace(groupName))
            {
                errors.Add("Polling group name cannot be empty when device name is also empty.");
            }
            else if (!pollingGroupNames.Add(groupName))
            {
                errors.Add($"Polling group '{groupName}' is registered more than once.");
            }

            if (string.IsNullOrWhiteSpace(group.DeviceName))
            {
                errors.Add($"Polling group '{groupName}' device name cannot be empty.");
            }
            else if (!deviceNames.Contains(group.DeviceName))
            {
                errors.Add($"Polling group '{groupName}' references unknown device '{group.DeviceName}'.");
            }

            if (group.Interval <= TimeSpan.Zero)
            {
                errors.Add($"Polling group '{groupName}' interval must be greater than zero.");
            }

            if (group.Enabled && group.Addresses.Count == 0 && group.SignalNames.Count == 0)
            {
                errors.Add($"Polling group '{groupName}' must contain at least one address or signal name.");
            }

            if (group.Addresses.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"Polling group '{groupName}' contains an empty address.");
            }

            if (group.SignalNames.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"Polling group '{groupName}' contains an empty signal name.");
            }

            if (devicesByName.TryGetValue(group.DeviceName, out var device))
            {
                foreach (var signalName in group.SignalNames.Where(signalName => !string.IsNullOrWhiteSpace(signalName)))
                {
                    if (!device.TryGetSignal(signalName, out _))
                    {
                        errors.Add(
                            $"Polling group '{groupName}' references unknown signal '{signalName}' on device '{group.DeviceName}'.");
                    }
                }
            }
        }

        return errors;
    }
}
