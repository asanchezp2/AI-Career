[CmdletBinding()]
param(
    [Uri]$BaseUrl = [Uri]::new('http://localhost:8080')
)

$ErrorActionPreference = 'Stop'
$transactionsUrl = [Uri]::new($BaseUrl, '/api/v1/transactions')

function New-Transaction {
    param(
        [Parameter(Mandatory)][Guid]$SourceAccountId,
        [Parameter(Mandatory)][decimal]$Value
    )

    $payload = @{
        sourceAccountId = $SourceAccountId
        targetAccountId = [Guid]::NewGuid()
        tranferTypeId = 1
        value = $Value
    } | ConvertTo-Json -Compress

    $created = Invoke-RestMethod -Uri $transactionsUrl -Method Post -ContentType 'application/json' -Body $payload -TimeoutSec 15
    if ($created.status -ne 'pending') {
        throw "POST must return pending; received '$($created.status)'."
    }

    return $created
}

function Wait-ForTerminalStatus {
    param([Parameter(Mandatory)][Guid]$TransactionId)

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        $state = Invoke-RestMethod -Uri "$($transactionsUrl.AbsoluteUri)/$TransactionId" -TimeoutSec 10
        if ($state.status -ne 'pending') {
            return $state
        }
        Start-Sleep -Milliseconds 250
    }

    throw "Transaction $TransactionId remained pending after 15 seconds."
}

function Assert-Outcome {
    param(
        [Parameter(Mandatory)]$State,
        [Parameter(Mandatory)][string]$ExpectedStatus,
        [string]$ExpectedReason
    )

    if ($State.status -ne $ExpectedStatus) {
        throw "Expected '$ExpectedStatus'; received '$($State.status)'."
    }
    if ($ExpectedReason -and $State.rejectionReason -ne $ExpectedReason) {
        throw "Expected rejection reason '$ExpectedReason'; received '$($State.rejectionReason)'."
    }
}

$readiness = Invoke-RestMethod -Uri ([Uri]::new($BaseUrl, '/health/ready')) -TimeoutSec 15
if ($readiness.status -ne 'Healthy') {
    throw "API is not ready: $($readiness | ConvertTo-Json -Compress)"
}

$results = [Collections.Generic.List[object]]::new()

$approvedCreated = New-Transaction -SourceAccountId ([Guid]::NewGuid()) -Value 120
$approved = Wait-ForTerminalStatus -TransactionId $approvedCreated.transactionExternalId
Assert-Outcome -State $approved -ExpectedStatus 'approved'
$results.Add([pscustomobject]@{ scenario = 'approval'; status = $approved.status; reason = $approved.rejectionReason; id = $approved.transactionExternalId })

$highValueCreated = New-Transaction -SourceAccountId ([Guid]::NewGuid()) -Value 2500
$highValue = Wait-ForTerminalStatus -TransactionId $highValueCreated.transactionExternalId
Assert-Outcome -State $highValue -ExpectedStatus 'rejected' -ExpectedReason 'highvalue'
$results.Add([pscustomobject]@{ scenario = 'high value'; status = $highValue.status; reason = $highValue.rejectionReason; id = $highValue.transactionExternalId })

$boundaryCreated = New-Transaction -SourceAccountId ([Guid]::NewGuid()) -Value 2000
$boundary = Wait-ForTerminalStatus -TransactionId $boundaryCreated.transactionExternalId
Assert-Outcome -State $boundary -ExpectedStatus 'approved'
$results.Add([pscustomobject]@{ scenario = 'high-value boundary (2000)'; status = $boundary.status; reason = $boundary.rejectionReason; id = $boundary.transactionExternalId })

$dailySource = [Guid]::NewGuid()
$dailyUtcDay = $null
for ($index = 0; $index -lt 10; $index++) {
    $created = New-Transaction -SourceAccountId $dailySource -Value 2000
    $state = Wait-ForTerminalStatus -TransactionId $created.transactionExternalId
    Assert-Outcome -State $state -ExpectedStatus 'approved'

    $createdUtcDay = ([DateTime]$state.createdAt).ToUniversalTime().Date
    if ($null -eq $dailyUtcDay) {
        $dailyUtcDay = $createdUtcDay
    }
    elseif ($createdUtcDay -ne $dailyUtcDay) {
        throw 'The daily accumulation scenario crossed a UTC day boundary; rerun it.'
    }
}

$dailyCreated = New-Transaction -SourceAccountId $dailySource -Value 1
$dailyRejected = Wait-ForTerminalStatus -TransactionId $dailyCreated.transactionExternalId
Assert-Outcome -State $dailyRejected -ExpectedStatus 'rejected' -ExpectedReason 'dailyaccumulated'
if (([DateTime]$dailyRejected.createdAt).ToUniversalTime().Date -ne $dailyUtcDay) {
    throw 'The final transaction fell on a different UTC date; rerun the daily scenario.'
}
$results.Add([pscustomobject]@{ scenario = 'daily accumulated (20001)'; status = $dailyRejected.status; reason = $dailyRejected.rejectionReason; id = $dailyRejected.transactionExternalId })

$results | Format-Table -AutoSize
Write-Output 'All local anti-fraud smoke scenarios passed.'